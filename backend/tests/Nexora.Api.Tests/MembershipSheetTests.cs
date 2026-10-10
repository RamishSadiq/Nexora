using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    [Fact]
    public async Task Primary_membership_is_nullable_audited_and_preserves_entitlement()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var path = $"/api/v1/membership/members/{Id(member)}"; var initial = await Json(await client.GetAsync(path + "/options")); Assert.Null(initial["options"]!["primaryMembership"]);
        var saved = await Json(await Send(client, path + "/options", HttpMethod.Put, new { version = Version(initial), options = new { primaryMembership = true }, reason = "Primary label confirmed from source" }));
        Assert.True(saved["options"]!["primaryMembership"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/options", HttpMethod.Put, new { version = Version(initial), options = new { primaryMembership = false }, reason = "Stale flag" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/options", HttpMethod.Put, new { version = Version(saved), options = new { primaryMembership = false }, reason = " " })).StatusCode);
        var detail = await Json(await client.GetAsync(path)); var current = detail["member"]!;
        foreach (var field in new[] { "status", "rate", "startsAtUtc", "endsAtUtc", "contactId", "accountId", "applicationId", "productId" }) Assert.Equal(member[field]?.ToJsonString(), current[field]?.ToJsonString());
        Assert.Contains("membership.options.updated", detail["history"]!.ToJsonString());
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.True(projection["options"]!["primaryMembership"]!.GetValue<bool>());
        // The primary label does not weaken the existing one-active-membership rule.
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = member["productId"]!.GetValue<string>(), applicationType = "individual", startsAtUtc = DateTime.UtcNow.Date, termsAccepted = true }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "Duplicate entitlement" })).StatusCode);
        saved = await Json(await Send(client, path + "/options", HttpMethod.Put, new { version = Version(saved), options = new { primaryMembership = false }, reason = "Recorded non-primary" })); Assert.False(saved["options"]!["primaryMembership"]!.GetValue<bool>());
        saved = await Json(await Send(client, path + "/options", HttpMethod.Put, new { version = Version(saved), options = new { }, reason = "Clear uncertain primary label" })); Assert.Null(saved["options"]!["primaryMembership"]);
    }

    private sealed record SheetIdentity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [Fact]
    public async Task Membership_timeline_closes_intervals_across_pages_and_preserves_unknown_dates()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (_, member) = await CreateMember(client);
        var session = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!; var memberId = Guid.Parse(Id(member));
        using (var scope = app.Services.CreateScope())
        {
            var scoped = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
            await using var db = new MembershipDbContext(new DbContextOptionsBuilder<MembershipDbContext>().UseSqlite(scoped.Database.GetDbConnection()).Options, new SheetIdentity(session.TenantId, session.UserId));
            var definition = new MembershipSetupValue { TenantId = session.TenantId, Kind = "member-category", Name = "Recorded category" }; db.Add(definition); await db.SaveChangesAsync();
            for (var i = 0; i < 30; i++) db.Add(new MembershipClassificationChange { TenantId = session.TenantId, MembershipId = memberId, Kind = "category", DefinitionId = definition.Id, NameSnapshot = "Category " + i, Sequence = i + 1, EffectiveOn = new DateOnly(2000, 1, 1).AddDays(i), ActorUserId = session.UserId, Reason = "Recorded category change" });
            db.Add(new MembershipClassificationChange { TenantId = session.TenantId, MembershipId = memberId, Kind = "category", Sequence = 31, EffectiveOn = new DateOnly(2000, 1, 30), ActorUserId = session.UserId, Reason = "Same-day clearing" });
            db.Add(new MembershipHistory { TenantId = session.TenantId, SubjectId = memberId, ActorUserId = session.UserId, Action = "membership.renew", EffectiveOn = DateOnly.FromDateTime(DateTime.UtcNow), CreatedAtUtc = DateTime.UtcNow.AddSeconds(1), Reason = "Renewal confirms active status" });
            db.Add(new MembershipHistory { TenantId = session.TenantId, SubjectId = memberId, ActorUserId = session.UserId, Action = "membership.cancel", Reason = "Undated legacy cancellation" });
            await db.SaveChangesAsync();
        }
        var path = $"/api/v1/membership/members/{memberId}/timeline";
        var first = await Json(await client.GetAsync(path + "?type=category")); Assert.Equal(31, first["total"]!.GetValue<int>()); Assert.Equal(25, first["items"]!.AsArray().Count);
        Assert.Null(first["items"]![0]!["value"]); Assert.Null(first["items"]![0]!["days"]); Assert.Equal(0, first["items"]![1]!["days"]!.GetValue<int>());
        var second = await Json(await client.GetAsync(path + "?type=category&page=2")); Assert.Equal(6, second["items"]!.AsArray().Count); Assert.Equal("2000-01-07", second["items"]![0]!["ceasedDate"]!.GetValue<string>()); Assert.Equal(1, second["items"]![0]!["days"]!.GetValue<int>());
        var statuses = await Json(await client.GetAsync(path + "?type=status")); Assert.Equal(3, statuses["total"]!.GetValue<int>());
        Assert.Equal(0, statuses["items"]![1]!["days"]!.GetValue<int>());
        var unknown = statuses["items"]!.AsArray().Single(row => row!["effectiveDate"] == null)!; Assert.Null(unknown["ceasedDate"]); Assert.Null(unknown["days"]);
        var all = await Json(await client.GetAsync(path)); Assert.Equal(34, all["total"]!.GetValue<int>());
        Assert.Empty((await Json(await client.GetAsync(path + "?page=3")))["items"]!.AsArray());
        foreach (var query in new[] { "?type=unknown", "?page=0", "?page=bad", "?page=1001" }) Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path + query)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/membership/members/{Guid.NewGuid()}/timeline")).StatusCode);
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path)).StatusCode);
        Assert.Equal(Version(member), Version((await Json(await client.GetAsync($"/api/v1/membership/members/{memberId}")))["member"]!));
    }
}
