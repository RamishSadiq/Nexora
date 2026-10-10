using System.Net;
using System.Text.Json.Nodes;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    private static async Task<JsonNode> SubmitRejoin(HttpClient client, JsonNode member, Guid? previous = null, string? contact = null, string? account = null) =>
        await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = contact ?? member["contactId"]!.GetValue<string>(), accountId = account, productId = member["productId"]!.GetValue<string>(), applicationType = account == null ? "individual" : "corporate", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true, previousMembershipId = previous ?? Guid.Parse(Id(member)) }));

    [Fact]
    public async Task Rejoin_creates_a_distinct_linear_membership_and_revalidates_approval()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var path = $"/api/v1/membership/members/{Id(member)}";
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = member["productId"]!.GetValue<string>(), applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true, previousMembershipId = Id(member) })).StatusCode);
        var join = await Json(await client.GetAsync(path + "/join-date")); join = await Json(await Send(client, path + "/join-date", HttpMethod.Put, new { version = Version(join), originalJoinDate = "1998-03-04", reason = "Original evidence" }));
        var cancelled = await Json(await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(join), reason = "Leaving" }));
        var candidates = await Json(await client.GetAsync($"/api/v1/membership/rejoin-candidates?contactId={Id(contact)}")); Assert.Single(candidates["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/membership/rejoin-candidates?contactId={Id(contact)}&page=0")).StatusCode);
        var first = await SubmitRejoin(client, member); var pending = await SubmitRejoin(client, member);
        Assert.Equal(Id(member), first["previousMembershipId"]!.GetValue<string>()); Assert.Equal("rejoin", first["lifecyclePurpose"]!.GetValue<string>());
        var reinstated = await Json(await Send(client, path + "/reinstate", HttpMethod.Post, new { version = Version(cancelled), reason = "Changed mind" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/api/v1/membership/applications/{Id(first)}/decision", HttpMethod.Post, new { version = Version(first), decision = "approve", reason = "Ineligible predecessor" })).StatusCode);
        cancelled = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(reinstated), reason = "Confirmed leave" }));
        var approved = await Json(await Send(client, $"/api/v1/membership/applications/{Id(first)}/decision", HttpMethod.Post, new { version = Version(first), decision = "approve", reason = "Rejoin approved" }));
        var nextPath = $"/api/v1/membership/members/{approved["membershipId"]!.GetValue<string>()}"; var next = (await Json(await client.GetAsync(nextPath)))["member"]!;
        Assert.NotEqual(Id(member), Id(next)); Assert.Equal(Id(member), next["previousMembershipId"]!.GetValue<string>()); Assert.Equal("1998-03-04", next["originalJoinDate"]!.GetValue<string>()); Assert.NotNull(next["rejoinedOn"]); Assert.Null(next["resignedOn"]); Assert.Null(next["reinstatedOn"]);
        Assert.Equal(cancelled.ToJsonString(), (await Json(await client.GetAsync(path)))["member"]!.ToJsonString());
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]!.AsArray().Single(x => x!["id"]!.GetValue<string>() == Id(next))!;
        Assert.Equal("membership.rejoin", projection["latestLifecycleAction"]!.GetValue<string>()); Assert.Equal(Id(member), projection["previousMembershipId"]!.GetValue<string>()); Assert.NotNull(projection["rejoinedOn"]);
        var stopped = await Json(await Send(client, nextPath + "/cancel", HttpMethod.Post, new { version = Version(next), reason = "Later leave" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/api/v1/membership/applications/{Id(pending)}/decision", HttpMethod.Post, new { version = Version(pending), decision = "approve", reason = "Cannot fork lineage" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/reinstate", HttpMethod.Post, new { version = Version(cancelled), reason = "Superseded membership" })).StatusCode);
        var following = await SubmitRejoin(client, stopped); Assert.Equal(Id(next), following["previousMembershipId"]!.GetValue<string>());
        var currentCandidates = await Json(await client.GetAsync($"/api/v1/membership/rejoin-candidates?contactId={Id(contact)}")); Assert.Equal(Id(next), Assert.Single(currentCandidates["items"]!.AsArray())!["id"]!.GetValue<string>());
        var other = await Contact(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(other), productId = member["productId"]!.GetValue<string>(), applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true, previousMembershipId = Id(next) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = member["productId"]!.GetValue<string>(), applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true, previousMembershipId = Guid.NewGuid() })).StatusCode);
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync($"/api/v1/membership/rejoin-candidates?contactId={Id(contact)}")).StatusCode);
    }

    [Fact]
    public async Task Organisation_rejoin_belongs_to_account_and_accepts_a_different_current_applicant()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var (contact, personal) = await CreateMember(client); var colleague = await Contact(client);
        var account = await Json(await Send(client, "/api/v1/crm/records", HttpMethod.Post, new { kind = "account", name = "Rejoining organisation", status = "active", category = "member" }), HttpStatusCode.Created);
        foreach (var applicant in new[] { contact, colleague }) await Json(await Send(client, $"/api/v1/crm/accounts/{Id(account)}/contacts", HttpMethod.Post, new { contactId = Id(applicant), startDate = DateTime.UtcNow.Date, workClassification = "unknown", roles = new[] { "membership" } }));
        var original = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), accountId = Id(account), productId = personal["productId"]!.GetValue<string>(), applicationType = "corporate", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true }));
        var approved = await Json(await Send(client, $"/api/v1/membership/applications/{Id(original)}/decision", HttpMethod.Post, new { version = Version(original), decision = "approve", reason = "Organisation approval" }));
        var path = $"/api/v1/membership/members/{approved["membershipId"]!.GetValue<string>()}"; var org = (await Json(await client.GetAsync(path)))["member"]!;
        org = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(org), reason = "Organisation left" }));
        var rejoin = await SubmitRejoin(client, org, contact: Id(colleague), account: Id(account));
        var decision = await Json(await Send(client, $"/api/v1/membership/applications/{Id(rejoin)}/decision", HttpMethod.Post, new { version = Version(rejoin), decision = "approve", reason = "Organisation rejoin" }));
        var next = (await Json(await client.GetAsync($"/api/v1/membership/members/{decision["membershipId"]!.GetValue<string>()}")))["member"]!;
        Assert.Equal(Id(account), next["accountId"]!.GetValue<string>()); Assert.Equal(Id(colleague), next["contactId"]!.GetValue<string>()); Assert.Null(next["originalJoinDate"]);
        var accountRows = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(account)}/memberships")))["items"]!.AsArray(); Assert.Equal(2, accountRows.Count);
        Assert.All(accountRows, row => Assert.Equal("organisation", row!["scope"]!.GetValue<string>()));
    }
}
