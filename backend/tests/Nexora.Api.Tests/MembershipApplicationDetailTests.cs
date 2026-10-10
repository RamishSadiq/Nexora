using System.Net;
using Nexora.Modules.Membership;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    [Fact]
    public async Task Application_requested_details_preserve_snapshots_and_apply_only_on_approval()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (_, existing) = await CreateMember(client); var contact = await Contact(client);
        var type = await Json(await Send(client, "/api/v1/membership/setup/member-type", HttpMethod.Post, new { name = "Original type", description = "" }));
        var category = await Json(await Send(client, "/api/v1/membership/setup/member-category", HttpMethod.Post, new { name = "Requested category", description = "" }));
        var band = await Json(await Send(client, "/api/v1/membership/setup/member-subscription-band", HttpMethod.Post, new { name = "Requested band", description = "" }));
        var invoice = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Requested invoice Account", status = "active" }), HttpStatusCode.Created);
        var details = new ApplicationRequestedDetails(Guid.Parse(Id(type)), Guid.Parse(Id(category)), Guid.Parse(Id(band)), " Bank transfer ", " Annual ", true, Guid.Parse(Id(invoice)), " Staff remarks\nSecond line ", " Staff description ");
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = existing["productId"]!.GetValue<string>(), applicationType = "individual", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date, applicantNotes = "Applicant supplied notes", requestedDetails = details }));
        var path = $"/api/v1/membership/applications/{Id(application)}"; var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        Assert.Equal("new", application["lifecyclePurpose"]!.GetValue<string>()); Assert.Equal(today, application["statusEffectiveOn"]!.GetValue<string>()); Assert.Null(application["membershipId"]);
        var initial = await Json(await client.GetAsync(path + "/details")); Assert.Equal("Original type", initial["names"]!["memberType"]!.GetValue<string>()); Assert.Equal("Bank transfer", initial["details"]!["paymentMethod"]!.GetValue<string>());
        await Json(await Send(client, $"/api/v1/membership/setup/member-type/{Id(type)}", HttpMethod.Patch, new { version = Version(type), name = "Renamed current type", description = "" }));
        var saved = await Json(await Send(client, path + "/details", HttpMethod.Put, new { version = Version(initial), details = details with { Description = "Reviewed description" }, reason = "Review requested details" }));
        Assert.Equal("Original type", saved["names"]!["memberType"]!.GetValue<string>()); Assert.Equal(today, saved["statusEffectiveOn"]!.GetValue<string>());
        var current = await Json(await client.GetAsync(path)); Assert.Equal(application["quotedRate"]!.ToJsonString(), current["quotedRate"]!.ToJsonString()); Assert.Equal(application["requestedEndUtc"]!.ToJsonString(), current["requestedEndUtc"]!.ToJsonString()); Assert.Equal("Applicant supplied notes", current["applicantNotes"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/details", HttpMethod.Put, new { version = Version(initial), details, reason = "Stale" })).StatusCode);
        foreach (var invalid in new[] { details with { MemberTypeId = Guid.Parse(Id(category)) }, details with { CategoryId = Guid.NewGuid() }, details with { InvoiceToAccount = false }, details with { InvoicingAccountId = null }, details with { InvoicingAccountId = Guid.Parse(Id(contact)) }, details with { Remarks = new string('a', 4001) }, details with { PaymentFrequency = "Bad\u0001" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/details", HttpMethod.Put, new { version = Version(saved), details = invalid, reason = "Invalid requested details" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/details", HttpMethod.Put, new { version = Version(saved), details, reason = " " })).StatusCode);
        var approved = await Json(await Send(client, path + "/decision", HttpMethod.Post, new { version = Version(saved), decision = "approve", reason = "Reviewed approval" })); Assert.Equal(today, approved["statusEffectiveOn"]!.GetValue<string>());
        var memberPath = "/api/v1/membership/members/" + approved["membershipId"]!.GetValue<string>(); var classifications = await Json(await client.GetAsync(memberPath + "/classifications"));
        Assert.Equal("Original type", classifications["current"]!["memberType"]!["name"]!.GetValue<string>()); Assert.Equal("Requested category", classifications["current"]!["category"]!["name"]!.GetValue<string>()); Assert.Equal("Requested band", classifications["current"]!["subscriptionBand"]!["name"]!.GetValue<string>()); Assert.Equal(3, classifications["total"]!.GetValue<int>());
        var arrangement = await Json(await client.GetAsync(memberPath + "/financial-arrangement")); Assert.Equal(Id(invoice), arrangement["details"]!["invoicingAccountId"]!.GetValue<string>()); Assert.Equal("Bank transfer", arrangement["details"]!["paymentMethod"]!.GetValue<string>());
        var member = (await Json(await client.GetAsync(memberPath)))["member"]!; Assert.Null(member["remarks"]); Assert.Equal(application["quotedRate"]!.ToJsonString(), member["rate"]!.ToJsonString()); Assert.Equal(Id(contact), member["contactId"]!.GetValue<string>()); Assert.Null(member["accountId"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/details", HttpMethod.Put, new { version = Version(approved), details = new { }, reason = "Cannot change approved snapshot" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/decision", HttpMethod.Post, new { version = Version(approved), decision = "approve", reason = "Duplicate approval" })).StatusCode);
    }

    [Fact]
    public async Task Application_details_clear_and_revalidate_invoice_account_before_approval()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (_, existing) = await CreateMember(client); var contact = await Contact(client);
        var invoice = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Invoice one", status = "active" }), HttpStatusCode.Created);
        var nextInvoice = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Invoice two", status = "active" }), HttpStatusCode.Created);
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = existing["productId"]!.GetValue<string>(), applicationType = "individual", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date, requestedDetails = new { invoiceToAccount = true, invoicingAccountId = Id(invoice), remarks = "Retained remarks" } }));
        var path = $"/api/v1/membership/applications/{Id(application)}";
        var saved = await Json(await Send(client, path + "/details", HttpMethod.Put, new { version = Version(application), details = new { invoiceToAccount = true, invoicingAccountId = Id(nextInvoice) }, reason = "Different invoice recipient" })); Assert.Equal("Invoice two", saved["names"]!["invoicingAccount"]!.GetValue<string>()); Assert.Null(saved["details"]!["remarks"]);
        await Json(await Send(client, $"/api/v1/crm/records/{Id(nextInvoice)}/archive", HttpMethod.Post, new { version = Version(nextInvoice) }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/decision", HttpMethod.Post, new { version = Version(saved), decision = "approve", reason = "Unavailable invoice Account" })).StatusCode);
        var unchanged = await Json(await client.GetAsync(path)); Assert.Equal("submitted", unchanged["status"]!.GetValue<string>()); Assert.Null(unchanged["membershipId"]); Assert.Equal(Version(saved), Version(unchanged));
        saved = await Json(await Send(client, path + "/details", HttpMethod.Put, new { version = Version(saved), details = new { }, reason = "Clear unconfirmed details" })); Assert.All(saved["details"]!.AsObject(), field => Assert.Null(field.Value));
        var rejected = await Json(await Send(client, path + "/decision", HttpMethod.Post, new { version = Version(saved), decision = "reject", reason = "Not eligible" })); Assert.Equal("rejected", rejected["status"]!.GetValue<string>()); Assert.NotNull(rejected["statusEffectiveOn"]); Assert.Null(rejected["membershipId"]);
        foreach (var purpose in new[] { "upgrade", "reinstate", "rejoin", "other" }) Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = existing["productId"]!.GetValue<string>(), applicationType = "individual", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date, lifecyclePurpose = purpose })).StatusCode);
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path + "/details")).StatusCode);
    }
}
