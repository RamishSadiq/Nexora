using System.Net;
using System.Text.Json.Nodes;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    [Fact]
    public async Task Financial_arrangements_validate_relationships_retain_evidence_and_preserve_entitlement()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var payer = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Payer company", status = "active" }), HttpStatusCode.Created);
        var recipient = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Invoice recipient", status = "active" }), HttpStatusCode.Created);
        var path = $"/api/v1/membership/members/{Id(member)}"; var initial = await Json(await client.GetAsync(path + "/financial-arrangement")); Assert.All(initial["details"]!.AsObject(), property => Assert.Null(property.Value));
        var saved = await Json(await Send(client, path + "/financial-arrangement", HttpMethod.Put, new { version = Version(member), details = new { renewalMonth = 11, renewalDate = "2001-02-03", nextRenewalDate = "2002-02-03", promotionCode = " LEGACY ", paymentMethod = " Bank transfer ", paymentFrequency = " Annual ", paymentProvider = " GoCardless ", paidById = Id(payer), invoiceToAccount = true, invoicingAccountId = Id(recipient), paidByName = "Forged snapshot" }, reason = "Recorded financial arrangement" }));
        Assert.Equal("gocardless", saved["details"]!["paymentProvider"]!.GetValue<string>());
        Assert.Equal("Payer company", saved["details"]!["paidByName"]!.GetValue<string>()); Assert.Equal("Invoice recipient", saved["details"]!["invoicingAccountName"]!.GetValue<string>()); Assert.Equal("LEGACY", saved["details"]!["promotionCode"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/financial-arrangement", HttpMethod.Put, new { version = Version(member), details = new { }, reason = "Stale" })).StatusCode);
        foreach (var invalid in new object[] { new { paymentProvider = "unsupported" }, new { renewalMonth = 0 }, new { renewalMonth = 13 }, new { renewalDate = "0001-01-01" }, new { renewalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) }, new { nextRenewalDate = "0001-01-01" }, new { renewalDate = "2001-02-03", nextRenewalDate = "2001-02-02" }, new { promotionCode = new string('a', 161) }, new { paymentMethod = "Bad\u0001" }, new { invoiceToAccount = true }, new { invoiceToAccount = false, invoicingAccountId = Id(recipient) }, new { invoiceToAccount = true, invoicingAccountId = Id(contact) } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/financial-arrangement", HttpMethod.Put, new { version = Version(saved), details = invalid, reason = "Invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, path + "/financial-arrangement", HttpMethod.Put, new { version = Version(saved), details = new { paidById = Guid.NewGuid() }, reason = "Unavailable payer" })).StatusCode);
        var current = (await Json(await client.GetAsync(path)))["member"]!;
        foreach (var field in new[] { "contactId", "accountId", "productId", "rate", "status", "startsAtUtc", "endsAtUtc" }) Assert.Equal(member[field]?.ToJsonString(), current[field]?.ToJsonString());
        var projected = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.Equal("Payer company", projected["financialArrangement"]!["paidByName"]!.GetValue<string>()); Assert.Equal("membership.activated", projected["latestLifecycleAction"]!.GetValue<string>());
        saved = await Json(await Send(client, path + "/financial-arrangement", HttpMethod.Put, new { version = Version(saved), details = new { paidById = Id(contact), invoiceToAccount = false, paymentProvider = "stripe" }, reason = "Contact pays instead" })); Assert.Equal("contact", saved["details"]!["paidByKind"]!.GetValue<string>()); Assert.Equal("stripe", saved["details"]!["paymentProvider"]!.GetValue<string>());
        saved = await Json(await Send(client, path + "/financial-arrangement", HttpMethod.Put, new { version = Version(saved), details = new { }, reason = "Clear unverifiable arrangement" })); Assert.All(saved["details"]!.AsObject(), property => Assert.Null(property.Value));
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path + "/financial-arrangement")).StatusCode);
    }
}
