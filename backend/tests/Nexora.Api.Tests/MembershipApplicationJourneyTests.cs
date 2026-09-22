using System.Net;
using System.Text.Json.Nodes;
using static Nexora.Api.Tests.WorkflowTestClient;
namespace Nexora.Api.Tests;
public sealed class MembershipApplicationJourneyTests
{
    [Fact]
    public async Task Approval_uses_the_submitted_quote_end_date_after_the_product_policy_changes()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var contact = await Contact(client);
        var product = await Json(await Send(client, "/api/v1/membership/products", HttpMethod.Post,
            new { name = "Fixed year", category = "Individual", termMonths = 12, rate = 120, currency = "GBP", isActive = true }));
        var policy = await Json(await Send(client, $"/api/v1/membership/products/{Id(product)}/policy", HttpMethod.Put,
            new { renewalMode = "fixed", yearEndMonth = 12, yearEndDay = 31, prorate = true, processingFee = 0, credit = 0 }));
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post,
            new { contactId = Id(contact), productId = Id(product), startsAtUtc = DateTime.UtcNow.Date, applicationType = "individual", termsAccepted = true }));
        var expectedEnd = new DateTime(DateTime.UtcNow.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expectedEnd, application["requestedEndUtc"]!.GetValue<DateTime>());

        await Json(await Send(client, $"/api/v1/membership/products/{Id(product)}/policy", HttpMethod.Put,
            new { version = Version(policy), renewalMode = "rolling", yearEndMonth = 12, yearEndDay = 31, prorate = false, processingFee = 50, credit = 0 }));
        var decision = await Json(await Send(client, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post,
            new { version = Version(application), decision = "approve", reason = "Approve original quote" }));
        var member = (await Json(await client.GetAsync("/api/v1/membership/members/" + decision["membershipId"]!.GetValue<string>())))["member"]!;
        Assert.Equal(expectedEnd, member["endsAtUtc"]!.GetValue<DateTime>());
        Assert.Equal(application["quotedRate"]!.GetValue<decimal>(), member["rate"]!.GetValue<decimal>());
    }

    [Fact]
    public async Task Organisation_requires_link_and_terms_then_approval_preserves_context_and_blocks_same_organisation_overlap()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var account = await Json(await Send(client,"/api/v1/crm/records",HttpMethod.Post,new {kind="account",name="Journey organisation",status="active"}),HttpStatusCode.Created);
        var contact = await Contact(client);
        var product = await Json(await Send(client,"/api/v1/membership/products",HttpMethod.Post,new {name="Organisation annual",category="Corporate",termMonths=12,rate=125,currency="GBP",isActive=true}));
        object Request(bool terms = true) => new {accountId=Id(account),contactId=Id(contact),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date,applicationType="corporate",termsAccepted=terms,applicantRole="Director",applicantNotes="Reviewed evidence"};
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,Request())).StatusCode);
        await Link(client,account,contact);
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,Request(false))).StatusCode);
        var application = await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,Request()));
        Assert.Equal("Director",application["applicantRole"]!.GetValue<string>());
        Assert.NotNull(application["termsAcceptedAtUtc"]);
        var approved = await Json(await Send(client,$"/api/v1/membership/applications/{Id(application)}/decision",HttpMethod.Post,new {version=Version(application),decision="approve",reason="Evidence checked"}));
        var member = (await Json(await client.GetAsync("/api/v1/membership/members/"+approved["membershipId"]!.GetValue<string>())))["member"]!;
        Assert.Equal(Id(account),member["accountId"]!.GetValue<string>());
        Assert.Equal(125,member["rate"]!.GetValue<decimal>());
        Assert.Equal(HttpStatusCode.Conflict,(await Send(client,$"/api/v1/membership/applications/{Id(application)}/decision",HttpMethod.Post,new {version=Version(application),decision="approve",reason="Retry"})).StatusCode);
        var other = await Contact(client); await Link(client,account,other);
        var duplicate = await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,new {accountId=Id(account),contactId=Id(other),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date,applicationType="corporate",termsAccepted=true}));
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,$"/api/v1/membership/applications/{Id(duplicate)}/decision",HttpMethod.Post,new {version=Version(duplicate),decision="approve",reason="Different contact same organisation"})).StatusCode);
        var search = await Json(await client.GetAsync("/api/v1/membership/applications?search=Journey%20organisation"));
        Assert.Equal(2, search["totalCount"]!.GetValue<int>());
    }
    [Fact]
    public async Task Ending_contact_link_before_review_blocks_approval_but_allows_rejection()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var account = await Json(await Send(client,"/api/v1/crm/records",HttpMethod.Post,new {kind="account",name="Former employer",status="active"}),HttpStatusCode.Created);
        var contact = await Contact(client); var link = await Link(client,account,contact);
        var product = await Json(await Send(client,"/api/v1/membership/products",HttpMethod.Post,new {name="Annual",category="Corporate",termMonths=12,rate=125,currency="GBP",isActive=true}));
        var application = await Json(await Send(client,"/api/v1/membership/applications",HttpMethod.Post,new {accountId=Id(account),contactId=Id(contact),productId=Id(product),startsAtUtc=DateTime.UtcNow.Date,applicationType="corporate",termsAccepted=true}));
        await Json(await Send(client,$"/api/v1/crm/account-contacts/{Id(link)}/end",HttpMethod.Post,new {version=Version(link),endDate=DateTime.UtcNow.Date,reason="Left organisation"}));
        Assert.Equal(HttpStatusCode.BadRequest,(await Send(client,$"/api/v1/membership/applications/{Id(application)}/decision",HttpMethod.Post,new {version=Version(application),decision="approve",reason="Review"})).StatusCode);
        var rejected = await Json(await Send(client,$"/api/v1/membership/applications/{Id(application)}/decision",HttpMethod.Post,new {version=Version(application),decision="reject",reason="Contact has left"}));
        Assert.Equal("rejected",rejected["status"]!.GetValue<string>()); Assert.Null(rejected["membershipId"]);
    }
    private static async Task<JsonNode> Link(HttpClient client,JsonNode account,JsonNode contact) => await Json(await Send(client,$"/api/v1/crm/accounts/{Id(account)}/contacts",HttpMethod.Post,new {contactId=Id(contact),startDate=DateTime.UtcNow.Date,workClassification="unknown",isMainContact=true,isAuthorisedContact=false,roles=new[]{"membership"}}));
}
