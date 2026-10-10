using System.Net;
using System.Net.Http.Json;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    private sealed record LifecycleTestIdentity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    [Fact]
    public async Task Undated_legacy_members_can_resign_historically_and_reject_dates_before_known_evidence()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var (_, member) = await CreateMember(client); var path = $"/api/v1/membership/members/{Id(member)}";
        await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(member), reason = "Replace test subject with legacy fixture" }));
        Guid legacyId;
        var session = (await client.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using (var scope = app.Services.CreateScope())
        {
            var scoped = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
            var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlite(scoped.Database.GetDbConnection()).Options;
            await using var db = new MembershipDbContext(options, new LifecycleTestIdentity(session.TenantId, session.UserId));
            var source = await db.Set<MemberSubscription>().SingleAsync(x => x.Id == Guid.Parse(Id(member)));
            var application = new MembershipApplication { TenantId = source.TenantId, ContactId = source.ContactId, ProductId = source.ProductId, ApplicationType = "individual", Status = "approved" };
            db.Add(application); await db.SaveChangesAsync();
            var legacy = new MemberSubscription { TenantId = source.TenantId, ContactId = source.ContactId, ProductId = source.ProductId, ApplicationId = application.Id, StartsAtUtc = new DateTime(1998, 3, 4), EndsAtUtc = DateTime.UtcNow.AddYears(1), OriginalJoinDate = new DateOnly(1998, 3, 4), Rate = 125, Currency = "GBP" };
            db.Add(legacy); await db.SaveChangesAsync(); legacyId = legacy.Id;
        }
        path = $"/api/v1/membership/members/{legacyId}";
        var legacyMember = (await Json(await client.GetAsync(path)))["member"]!; Assert.Null(legacyMember["statusEffectiveOn"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(legacyMember), effectiveOn = "1997-01-01", reason = "Before joining" })).StatusCode);
        var resigned = await Json(await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(legacyMember), effectiveOn = "2001-02-03", reason = "Historical resignation evidence" }));
        Assert.Equal("2001-02-03", resigned["statusEffectiveOn"]!.GetValue<string>());
        var reinstated = await Json(await Send(client, path + "/reinstate", HttpMethod.Post, new { version = Version(resigned), reason = "Current reinstatement" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(reinstated), effectiveOn = "2002-02-03", reason = "Out of order" })).StatusCode);
        var receipt = (await Json(await client.GetAsync(path)))["history"]!.AsArray().Single(x => x!["action"]!.GetValue<string>() == "membership.resign")!;
        Assert.Equal("2001-02-03", receipt["effectiveOn"]!.GetValue<string>()); Assert.True(receipt["createdAtUtc"]!.GetValue<DateTime>() > new DateTime(2020, 1, 1));
    }

    [Fact]
    public async Task Effective_lifecycle_dates_preserve_receipts_and_distinguish_resignation_from_cancellation()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var (contact, member) = await CreateMember(client); var path = $"/api/v1/membership/members/{Id(member)}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        Assert.Equal(today, member["statusEffectiveOn"]!.GetValue<string>()); Assert.Null(member["resignedOn"]);
        foreach (var date in new[] { "0001-01-01", "2001-02-03", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd") })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(member), reason = "Invalid effective date", effectiveOn = date })).StatusCode);
        var resigned = await Json(await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(member), reason = "Leaving the association", effectiveOn = today }));
        Assert.Equal("cancelled", resigned["status"]!.GetValue<string>()); Assert.Equal(today, resigned["resignedOn"]!.GetValue<string>()); Assert.Equal("Leaving the association", resigned["resignationReason"]!.GetValue<string>());
        Assert.Equal(member["endsAtUtc"]!.ToJsonString(), resigned["endsAtUtc"]!.ToJsonString());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(member), reason = "Stale" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/resign", HttpMethod.Post, new { version = Version(resigned), reason = "Already resigned" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/reinstate", HttpMethod.Post, new { version = Version(resigned), reason = "Backdated rate", effectiveOn = "2001-02-03" })).StatusCode);
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!;
        Assert.Equal("membership.resign", projection["latestLifecycleAction"]!.GetValue<string>()); Assert.Equal(today, projection["latestLifecycleEffectiveOn"]!.GetValue<string>()); Assert.Equal(today, projection["lifecycleDates"]!["resignDate"]!.GetValue<string>());
        var reinstated = await Json(await Send(client, path + "/reinstate", HttpMethod.Post, new { version = Version(resigned), reason = "Reinstatement" }));
        Assert.Equal(today, reinstated["reinstatedOn"]!.GetValue<string>()); Assert.Equal(today, reinstated["resignedOn"]!.GetValue<string>());
        var cancelled = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(reinstated), reason = "Administrative cancellation" }));
        Assert.Equal("Leaving the association", cancelled["resignationReason"]!.GetValue<string>());
        var detail = await Json(await client.GetAsync(path)); var receipt = detail["history"]!.AsArray().Single(x => x!["action"]!.GetValue<string>() == "membership.resign")!;
        Assert.Equal(today, receipt["effectiveOn"]!.GetValue<string>()); Assert.NotNull(receipt["createdAtUtc"]);
        using var outside = await Login(app, "outside@nexora.test"); Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path + "/resign", HttpMethod.Post, new { version = Version(cancelled), reason = "Forbidden" })).StatusCode);
    }

    [Fact]
    public async Task Active_renewal_preserves_status_date_and_does_not_fabricate_resignation_or_reinstatement()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app);
        var (contact, member) = await CreateMember(client); var path = $"/api/v1/membership/members/{Id(member)}";
        var renewed = await Json(await Send(client, path + "/renew", HttpMethod.Post, new { version = Version(member), reason = "Renewal receipt" }));
        Assert.Equal(member["statusEffectiveOn"]!.ToJsonString(), renewed["statusEffectiveOn"]!.ToJsonString()); Assert.Null(renewed["resignedOn"]); Assert.Null(renewed["reinstatedOn"]);
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!; Assert.Equal("membership.renew", projection["latestLifecycleAction"]!.GetValue<string>());
    }
}
