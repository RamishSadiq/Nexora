using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class AdministrationTests
{
    private static string Stamp(JsonNode row) => row["concurrencyStamp"]!.GetValue<string>();
    private static async Task<JsonNode> Group(HttpClient client, string name, string[] permissions) => await Json(await Send(client, "/api/v1/admin/security-groups", HttpMethod.Post, new { name, description = "Test group", permissions }));
    private static async Task<JsonNode> User(HttpClient client, string email, string groupId) => await Json(await Send(client, "/api/v1/admin/users", HttpMethod.Post, new { displayName = "Test user", email, isActive = true, groupIds = new[] { groupId }, password = IdentityApiFactory.AdminPassword }));

    [Fact]
    public async Task Users_can_be_created_edited_reset_and_deleted_without_disclosing_passwords()
    {
        using var app = new IdentityApiFactory(); using var admin = await Login(app);
        var group = await Group(admin, "CRM viewers", ["crm.read"]);
        var user = await User(admin, "staff@nexora.test", Id(group));
        Assert.True(user["hasPassword"]!.GetValue<bool>());
        Assert.Null(user["password"]); Assert.Null(user["passwordHash"]);
        using var staff = await Login(app, "staff@nexora.test");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/v1/admin/users")).StatusCode);
        var edited = await Json(await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Patch, new { displayName = "Updated user", email = "renamed@nexora.test", isActive = true, groupIds = new[] { Id(group) }, concurrencyStamp = Stamp(user), password = "Changed-Pass-2026!" }));
        Assert.NotEqual(Stamp(user), Stamp(edited));
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/v1/auth/session")).StatusCode);
        using var renamed = app.CreateClient();
        await Json(await Send(renamed, "/api/v1/auth/login", HttpMethod.Post, new LoginRequest("renamed@nexora.test", "Changed-Pass-2026!", false)));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Delete, new { concurrencyStamp = Stamp(user) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Delete, new { concurrencyStamp = Stamp(edited) })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await renamed.GetAsync("/api/v1/auth/session")).StatusCode);
        Assert.DoesNotContain((await Json(await admin.GetAsync("/api/v1/admin/users"))).AsArray(), row => Id(row!) == Id(user));
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var access = db.BeginSystemAccess();
        Assert.Contains(await db.AuditEvents.ToListAsync(), row => row.EventType == "identity.user.deleted" && row.Subject == Id(user));
    }

    [Fact]
    public async Task Security_group_changes_revoke_and_grant_access_to_existing_sessions()
    {
        using var app = new IdentityApiFactory(); using var admin = await Login(app);
        var group = await Group(admin, "Limited staff", ["crm.read"]);
        var user = await User(admin, "limited@nexora.test", Id(group)); using var staff = await Login(app, "limited@nexora.test");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/v1/membership/members")).StatusCode);
        var updated = await Json(await Send(admin, "/api/v1/admin/security-groups/" + Id(group), HttpMethod.Patch, new { name = "Membership viewers", description = "Limited", permissions = new[] { "membership.read" }, concurrencyStamp = Stamp(group) }));
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/membership/members")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(staff, "/api/v1/membership/setup/member-type", HttpMethod.Post, new { name = "Denied", description = "" })).StatusCode);
        var session = await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session");
        Assert.Equal(new[] { "membership.read" }, session!.Permissions); Assert.Contains("Membership viewers", session.Roles);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/security-groups/" + Id(group), HttpMethod.Delete, new { concurrencyStamp = Stamp(updated) })).StatusCode);
        var disabled = await Json(await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Patch, new { displayName = "Disabled", email = "limited@nexora.test", isActive = false, groupIds = new[] { Id(group) }, concurrencyStamp = Stamp(user) }));
        Assert.Equal(HttpStatusCode.Unauthorized, (await staff.GetAsync("/api/v1/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Delete, new { concurrencyStamp = Stamp(disabled) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, "/api/v1/admin/security-groups/" + Id(group), HttpMethod.Delete, new { concurrencyStamp = Stamp(updated) })).StatusCode);
    }

    [Fact]
    public async Task Admin_users_and_groups_are_protected_and_invalid_writes_are_atomic()
    {
        using var app = new IdentityApiFactory(); using var admin = await Login(app);
        var users = (await Json(await admin.GetAsync("/api/v1/admin/users"))).AsArray(); var user = users.Single()!;
        var groups = (await Json(await admin.GetAsync("/api/v1/admin/security-groups"))).AsArray(); var group = groups.Single()!;
        Assert.True(user["isProtected"]!.GetValue<bool>()); Assert.True(group["isSystem"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Delete, new { concurrencyStamp = Stamp(user) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Patch, new { displayName = "Admin", email = IdentityApiFactory.AdminEmail, isActive = false, groupIds = new[] { Id(group) }, concurrencyStamp = Stamp(user) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/security-groups/" + Id(group), HttpMethod.Patch, new { name = "Admin", description = "", permissions = new[] { "crm.read" }, concurrencyStamp = Stamp(group) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/security-groups/" + Id(group), HttpMethod.Delete, new { concurrencyStamp = Stamp(group) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/security-groups", HttpMethod.Post, new { name = "Invalid", description = "", permissions = new[] { "crm.manage" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/security-groups", HttpMethod.Post, new { name = "Unknown", description = "", permissions = new[] { "unknown.read" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/users", HttpMethod.Post, new { displayName = "Weak", email = "weak@nexora.test", isActive = true, groupIds = new[] { Id(group) }, password = "weak" })).StatusCode);
        Assert.Single((await Json(await admin.GetAsync("/api/v1/admin/users"))).AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/v1/admin/security-groups", new { name = "No CSRF", description = "", permissions = new[] { "crm.read" } })).StatusCode);
    }

    [Fact]
    public async Task User_group_reassignment_combines_permissions_and_protects_other_admin_accounts()
    {
        using var app = new IdentityApiFactory(); using var admin = await Login(app);
        var crm = await Group(admin, "Relationships viewers", ["crm.read"]);
        var membership = await Group(admin, "Membership viewers", ["membership.read"]);
        var user = await User(admin, "combined@nexora.test", Id(crm)); using var staff = await Login(app, "combined@nexora.test");
        user = await Json(await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Patch, new { displayName = "Combined", email = "combined@nexora.test", isActive = true, groupIds = new[] { Id(crm), Id(membership) }, concurrencyStamp = Stamp(user) }));
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/membership/members")).StatusCode);
        await Json(await Send(admin, "/api/v1/admin/users/" + Id(user), HttpMethod.Patch, new { displayName = "Membership only", email = "combined@nexora.test", isActive = true, groupIds = new[] { Id(membership) }, concurrencyStamp = Stamp(user) }));
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/v1/crm/records")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/membership/members")).StatusCode);
        var system = (await Json(await admin.GetAsync("/api/v1/admin/security-groups"))).AsArray().Single(row => row!["isSystem"]!.GetValue<bool>())!;
        await User(admin, "second-admin@nexora.test", Id(system)); using var second = await Login(app, "second-admin@nexora.test");
        var originalAdmin = (await Json(await second.GetAsync("/api/v1/admin/users"))).AsArray().Single(row => row!["email"]!.GetValue<string>() == IdentityApiFactory.AdminEmail)!;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(second, "/api/v1/admin/users/" + Id(originalAdmin), HttpMethod.Delete, new { concurrencyStamp = Stamp(originalAdmin) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(second, "/api/v1/admin/users/" + Id(originalAdmin), HttpMethod.Patch, new { displayName = "Demoted", email = IdentityApiFactory.AdminEmail, isActive = true, groupIds = new[] { Id(crm) }, concurrencyStamp = Stamp(originalAdmin) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Tenant_boundaries_and_read_only_administration_are_enforced()
    {
        using var app = new IdentityApiFactory(); using var admin = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var otherGroup = (await Json(await outside.GetAsync("/api/v1/admin/security-groups"))).AsArray().Single()!;
        var otherUser = (await Json(await outside.GetAsync("/api/v1/admin/users"))).AsArray().Single()!;
        Assert.Equal(HttpStatusCode.NotFound, (await Send(admin, "/api/v1/admin/users/" + Id(otherUser), HttpMethod.Delete, new { concurrencyStamp = Stamp(otherUser) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(admin, "/api/v1/admin/security-groups/" + Id(otherGroup), HttpMethod.Patch, new { name = "Foreign", description = "", permissions = new[] { "crm.read" }, concurrencyStamp = Stamp(otherGroup) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, "/api/v1/admin/users", HttpMethod.Post, new { displayName = "Foreign role", email = "foreign-role@nexora.test", isActive = true, groupIds = new[] { Id(otherGroup) }, password = IdentityApiFactory.AdminPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, "/api/v1/admin/security-groups", HttpMethod.Post, new { name = "Denied", description = "", permissions = new[] { "crm.read" } })).StatusCode);
        Assert.DoesNotContain((await Json(await admin.GetAsync("/api/v1/admin/users"))).AsArray(), row => Id(row!) == Id(otherUser));
    }
}
