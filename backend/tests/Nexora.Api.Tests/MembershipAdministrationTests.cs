using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed partial class MembershipAdministrationTests
{
    private static async Task<(JsonNode Contact, JsonNode Member)> CreateMember(HttpClient client)
    {
        var contact = await Contact(client);
        var product = await Json(await Send(client, "/api/v1/membership/products", HttpMethod.Post, new { name = "Administration", category = "Standard", termMonths = 12, rate = 125, currency = "GBP", isActive = true }));
        var application = await Json(await Send(client, "/api/v1/membership/applications", HttpMethod.Post, new { contactId = Id(contact), productId = Id(product), applicationType = "individual", termsAccepted = true, startsAtUtc = DateTime.UtcNow.Date }));
        var approved = await Json(await Send(client, $"/api/v1/membership/applications/{Id(application)}/decision", HttpMethod.Post, new { version = Version(application), decision = "approve", reason = "Eligible" }));
        var member = (await Json(await client.GetAsync($"/api/v1/membership/members/{approved["membershipId"]!.GetValue<string>()}")))["member"]!;
        return (contact, member);
    }

    [Fact]
    public async Task Membership_administration_round_trips_validates_clears_and_preserves_lifecycle_values()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); var (contact, member) = await CreateMember(client);
        var path = $"/api/v1/membership/members/{Id(member)}"; var initial = await Json(await client.GetAsync(path + "/administration"));
        Assert.All(initial["details"]!.AsObject(), property => Assert.Null(property.Value));
        var details = new { legacyMemberReference = " MEM-0042 ", membershipPackSent = true, membershipPackSentDate = "2001-02-03", remarks = " Historical reference\nPack sent manually " };
        var saved = await Json(await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(initial), details }));
        Assert.NotEqual(Version(initial), Version(saved)); Assert.Equal("MEM-0042", saved["details"]!["legacyMemberReference"]!.GetValue<string>());
        Assert.Equal("Historical reference\nPack sent manually", saved["details"]!["remarks"]!.GetValue<string>()); Assert.Equal("2001-02-03", saved["details"]!["membershipPackSentDate"]!.GetValue<string>());
        Assert.True(saved["details"]!["membershipPackSent"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(initial), details })).StatusCode);
        foreach (var invalid in new object[] { new { legacyMemberReference = new string('a', 161) }, new { legacyMemberReference = "bad\u0001" }, new { remarks = new string('a', 4001) }, new { remarks = "bad\u0001" }, new { membershipPackSent = false, membershipPackSentDate = "2001-02-03" }, new { membershipPackSentDate = "2001-02-03" }, new { membershipPackSent = true, membershipPackSentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) }, new { membershipPackSent = true, membershipPackSentDate = "0001-01-01" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(saved), details = invalid })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(saved) })).StatusCode);
        var projection = (await Json(await client.GetAsync($"/api/v1/crm/records/{Id(contact)}/memberships")))["items"]![0]!;
        Assert.Equal("MEM-0042", projection["administration"]!["legacyMemberReference"]!.GetValue<string>()); Assert.Equal("membership.activated", projection["latestLifecycleAction"]!.GetValue<string>());
        var current = (await Json(await client.GetAsync(path)))["member"]!;
        foreach (var field in new[] { "status", "startsAtUtc", "endsAtUtc", "rate", "applicationId", "contactId", "productId" }) Assert.Equal(member[field]!.ToJsonString(), current[field]!.ToJsonString());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(member), reason = "Stale lifecycle" })).StatusCode);
        var cancelled = await Json(await Send(client, path + "/cancel", HttpMethod.Post, new { version = Version(current), reason = "Cancellation" }));
        Assert.Equal("MEM-0042", cancelled["legacyMemberReference"]!.GetValue<string>()); Assert.True(cancelled["membershipPackSent"]!.GetValue<bool>());
        saved = await Json(await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(cancelled), details = new { membershipPackSent = false } }));
        Assert.False(saved["details"]!["membershipPackSent"]!.GetValue<bool>()); Assert.Null(saved["details"]!["membershipPackSentDate"]); Assert.Null(saved["details"]!["legacyMemberReference"]);
        saved = await Json(await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(saved), details = new { membershipPackSent = true } })); Assert.True(saved["details"]!["membershipPackSent"]!.GetValue<bool>()); Assert.Null(saved["details"]!["membershipPackSentDate"]);
        saved = await Json(await Send(client, path + "/administration", HttpMethod.Put, new { version = Version(saved), details = new { } })); Assert.All(saved["details"]!.AsObject(), property => Assert.Null(property.Value));
        Assert.Equal("cancelled", (await Json(await client.GetAsync(path)))["member"]!["status"]!.GetValue<string>());
        Assert.Contains("membership.administration.updated", (await Json(await client.GetAsync(path)))["history"]!.ToJsonString());
    }

    [Fact]
    public async Task Membership_administration_rejects_foreign_and_unauthorised_requests()
    {
        using var app = new IdentityApiFactory(); using var client = await Login(app); using var outside = await Login(app, "outside@nexora.test"); var (_, member) = await CreateMember(client);
        var path = $"/api/v1/membership/members/{Id(member)}/administration";
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, path, HttpMethod.Put, new { version = Version(member), details = new { remarks = "Forbidden" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/membership/members/{Guid.NewGuid()}/administration")).StatusCode);
        using var anonymous = app.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
            var outsideRole = await db.Roles.SingleAsync(x => x.Name == "Outside Administrator");
            db.RolePermissions.AddRange(new RolePermission { RoleId = outsideRole.Id, Permission = "membership.read" }, new RolePermission { RoleId = outsideRole.Id, Permission = "membership.manage" });
            await db.SaveChangesAsync();
        }
        using var foreignManager = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.NotFound, (await foreignManager.GetAsync(path.Replace("/administration", "/timeline"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(foreignManager, path.Replace("/administration", "/financial-arrangement"), HttpMethod.Put, new { version = Version(member), details = new { }, reason = "Foreign" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(foreignManager, path.Replace("/administration", "/options"), HttpMethod.Put, new { version = Version(member), options = new { }, reason = "Foreign" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(foreignManager, path.Replace("/administration", "/archive"), HttpMethod.Put, new { version = Version(member), isArchived = true, reason = "Foreign" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await foreignManager.GetAsync(path.Replace("/administration", "/join-date"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(foreignManager, path.Replace("/administration", "/join-date"), HttpMethod.Put, new { version = Version(member), originalJoinDate = "2001-02-03", reason = "Foreign" })).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await foreignManager.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(foreignManager, path, HttpMethod.Put, new { version = Version(member), details = new { remarks = "Foreign" } })).StatusCode);
        var applicationDetailsPath = "/api/v1/membership/applications/" + member["applicationId"]!.GetValue<string>() + "/details";
        Assert.Equal(HttpStatusCode.NotFound, (await foreignManager.GetAsync(applicationDetailsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(foreignManager, applicationDetailsPath, HttpMethod.Put, new { version = Version(member), details = new { }, reason = "Foreign" })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
            var role = await db.Roles.SingleAsync(x => x.Name == "Outside Administrator");
            db.Remove(await db.RolePermissions.SingleAsync(x => x.RoleId == role.Id && x.Permission == "membership.manage")); await db.SaveChangesAsync();
        }
        using var reader = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, applicationDetailsPath, HttpMethod.Put, new { version = Version(member), details = new { }, reason = "Read only" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, path.Replace("/administration", "/financial-arrangement"), HttpMethod.Put, new { version = Version(member), details = new { }, reason = "Read only" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, path.Replace("/administration", "/options"), HttpMethod.Put, new { version = Version(member), options = new { }, reason = "Read only" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, path.Replace("/administration", "/archive"), HttpMethod.Put, new { version = Version(member), isArchived = true, reason = "Read only" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, path.Replace("/administration", "/join-date"), HttpMethod.Put, new { version = Version(member), originalJoinDate = "2001-02-03", reason = "Read only" })).StatusCode); Assert.Equal(HttpStatusCode.NotFound, (await reader.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(reader, path, HttpMethod.Put, new { version = Version(member), details = new { remarks = "Read only" } })).StatusCode);
    }
}
