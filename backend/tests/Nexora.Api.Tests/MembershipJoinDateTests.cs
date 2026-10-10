using System.Net;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    [Fact]
    public async Task Original_join_date_is_explicit_validated_versioned_and_preserved_by_lifecycle()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var (contact, member) = await CreateMember(client); var path = $"/api/v1/membership/members/{Id(member)}";
        var initial = await Json(await client.GetAsync(path + "/join-date")); Assert.Null(initial["originalJoinDate"]);
        foreach (var invalid in new object[] {
            new { originalJoinDate = "0001-01-01", reason = "Invalid" },
            new { originalJoinDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), reason = "Future" },
            new { originalJoinDate = "2001-02-03", reason = " " },
            new { originalJoinDate = "2001-02-03", reason = "bad\u0001" },
            new { originalJoinDate = "2001-02-03", reason = new string('a', 501) } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/join-date", HttpMethod.Put, new { version = Version(initial), originalJoinDate = System.Text.Json.JsonSerializer.SerializeToNode(invalid)!["originalJoinDate"], reason = System.Text.Json.JsonSerializer.SerializeToNode(invalid)!["reason"] })).StatusCode);
        var saved = await Json(await Send(client, path + "/join-date", HttpMethod.Put, new { version = Version(initial), originalJoinDate = "2001-02-03", reason = "Historical evidence" }));
        Assert.NotEqual(Version(initial), Version(saved)); Assert.Equal("2001-02-03", saved["originalJoinDate"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/join-date", HttpMethod.Put, new { version = Version(initial), originalJoinDate = "2001-02-04", reason = "Stale" })).StatusCode);
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!;
        Assert.Equal("2001-02-03", projection["originalJoinDate"]!.GetValue<string>()); Assert.Equal("membership.activated", projection["latestLifecycleAction"]!.GetValue<string>());
        var current = (await Json(await client.GetAsync(path)))["member"]!;
        foreach (var field in new[] { "status", "startsAtUtc", "endsAtUtc", "rate", "applicationId" }) Assert.Equal(member[field]!.ToJsonString(), current[field]!.ToJsonString());
        var cancelled = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(current), reason = "Cancel" })); Assert.Equal("2001-02-03", cancelled["originalJoinDate"]!.GetValue<string>());
        var cleared = await Json(await Send(client, path + "/join-date", HttpMethod.Put, new { version = Version(cancelled), originalJoinDate = (string?)null, reason = "Unverified evidence" })); Assert.Null(cleared["originalJoinDate"]);
        Assert.Equal("cancelled", (await Json(await client.GetAsync(path)))["member"]!["status"]!.GetValue<string>());
        var history = (await Json(await client.GetAsync(path)))["history"]!.ToJsonString(); Assert.Contains("Historical evidence", history); Assert.Contains("Unverified evidence", history);
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path + "/join-date")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path + "/join-date", HttpMethod.Put, new { version = Version(cleared), originalJoinDate = "2001-02-03", reason = "Forbidden" })).StatusCode);
        using var anonymous = app.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path + "/join-date")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/membership/members/{Guid.NewGuid()}/join-date")).StatusCode);
    }
}
