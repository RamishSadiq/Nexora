using System.Net;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    [Fact]
    public async Task Communication_drafting_shares_archive_version_and_idempotent_retries_do_not_touch_it()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (_, member) = await CreateMember(client);
        var path = $"/api/v1/membership/members/{Id(member)}";
        var inactive = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(member), reason = "Inactive communication fixture" }));
        var request = new { requestId = Guid.NewGuid(), kind = "general", recipient = "person@example.test", subject = "Historical enquiry", body = "A valid draft before archive" };
        var draft = await Json(await Send(client, path + "/communications", HttpMethod.Post, request));
        var current = (await Json(await client.GetAsync(path)))["member"]!; Assert.NotEqual(Version(inactive), Version(current));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(inactive), isArchived = true, reason = "Archive stale draft snapshot" })).StatusCode);
        var retry = await Json(await Send(client, path + "/communications", HttpMethod.Post, request)); Assert.Equal(Id(draft), Id(retry)); Assert.Equal(Version(current), Version((await Json(await client.GetAsync(path)))["member"]!));
        await Json(await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(current), isArchived = true, reason = "Retain the draft" }));
        Assert.Single((await Json(await client.GetAsync(path + "/operations")))["communications"]!.AsArray());
    }

    [Fact]
    public async Task Archive_retains_history_blocks_operations_and_restores_without_reactivating()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var path = $"/api/v1/membership/members/{Id(member)}";
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(member), isArchived = true, reason = "Active cannot archive" })).StatusCode);
        var inactive = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(member), reason = "Membership ended" }));
        var pending = await SubmitRejoin(client, inactive);
        foreach (var reason in new[] { " ", "bad\u0001", new string('a', 501) }) Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(inactive), isArchived = true, reason })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(inactive), reason = "Missing state" })).StatusCode);
        var archived = await Json(await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(inactive), isArchived = true, reason = " Retained historical membership " }));
        Assert.True(archived["isArchived"]!.GetValue<bool>()); Assert.NotNull(archived["archivedAtUtc"]); Assert.NotNull(archived["archivedByUserId"]); Assert.Equal("Retained historical membership", archived["archiveReason"]!.GetValue<string>());
        foreach (var field in new[] { "status", "statusEffectiveOn", "startsAtUtc", "endsAtUtc", "rate", "applicationId" }) Assert.Equal(inactive[field]!.ToJsonString(), archived[field]!.ToJsonString());
        Assert.Empty((await Json(await client.GetAsync("/api/v1/membership/members")))["items"]!.AsArray());
        Assert.Single((await Json(await client.GetAsync("/api/v1/membership/members?archive=archived")))["items"]!.AsArray());
        Assert.Single((await Json(await client.GetAsync("/api/v1/membership/members?archive=all")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/membership/members?archive=invalid")).StatusCode);
        Assert.Contains("membership.archived", (await Json(await client.GetAsync(path)))["history"]!.ToJsonString());
        var projected = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.True(projected["isArchived"]!.GetValue<bool>()); Assert.Equal("membership.cancel", projected["latestLifecycleAction"]!.GetValue<string>());
        Assert.Empty((await Json(await client.GetAsync($"/api/v1/membership/rejoin-candidates?contactId={Id(contact)}")))["items"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/api/v1/membership/applications/{Id(pending)}/decision", HttpMethod.Post, new { version = Version(pending), decision = "approve", reason = "Archived predecessor" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/reinstate", HttpMethod.Post, new { version = Version(archived), reason = "Must restore" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/review", HttpMethod.Put, new { memberVersion = Version(archived), complianceStatus = "pending" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/certificates", HttpMethod.Post, new { version = Version(archived) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/communications", HttpMethod.Post, new { requestId = Guid.NewGuid(), kind = "general", recipient = "person@example.test", subject = "Historical", body = "No operational draft" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(inactive), isArchived = false, reason = "Stale" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(archived), isArchived = true, reason = "Already archived" })).StatusCode);
        var metadata = await Json(await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(archived), details = new { remarks = "Corrected historical evidence" } }));
        var restored = await Json(await Send(client, path + "/archive", HttpMethod.Put, new { version = Version(metadata), isArchived = false, reason = "Restore for rejoin review" }));
        Assert.False(restored["isArchived"]!.GetValue<bool>()); Assert.Null(restored["archivedAtUtc"]); Assert.Null(restored["archivedByUserId"]); Assert.Null(restored["archiveReason"]); Assert.Equal("cancelled", restored["status"]!.GetValue<string>());
        Assert.Contains("membership.restored", (await Json(await client.GetAsync(path)))["history"]!.ToJsonString());
        await Json(await Send(client, $"/api/v1/membership/applications/{Id(pending)}/decision", HttpMethod.Post, new { version = Version(pending), decision = "approve", reason = "Restored predecessor" }));
    }
}
