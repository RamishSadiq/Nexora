using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Content;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;
public sealed partial class PortalEndpointTests
{
    [Fact]
    public async Task Portal_configuration_is_published_only_versioned_and_requires_active_member_access()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var member = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/portal/configuration")).StatusCode);
        var (grant, _) = await Grant(app, staff); await PortalLogin(member);
        var defaults = await member.GetFromJsonAsync<PortalPresentation>("/api/v1/portal/configuration");
        Assert.Equal("Member portal", defaults!.Name);
        var draft = defaults with { Name = "Our member space", Pages = [new("member-guide", "Member guide", "Members only information")], Menu = [.. defaults.Menu, new("member-guide", "Guide", true)] };
        var saved = await Json(await Send(staff, "/api/v1/content/portal", HttpMethod.Put, new ContentModule.PortalConfigurationRequest(null, draft)));
        Assert.Equal("Member portal", (await member.GetFromJsonAsync<PortalPresentation>("/api/v1/portal/configuration"))!.Name);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/v1/content/portal", HttpMethod.Put, new ContentModule.PortalConfigurationRequest(null, draft))).StatusCode);
        var published = await Json(await Send(staff, "/api/v1/content/portal/publish", HttpMethod.Post, new { version = Version(saved), configuration = draft }));
        var liveResponse = await member.GetAsync("/api/v1/portal/configuration");
        Assert.Equal("no-store", liveResponse.Headers.CacheControl!.ToString());
        var live = await liveResponse.Content.ReadFromJsonAsync<PortalPresentation>();
        Assert.Equal(draft.Name, live!.Name); Assert.Single(live.Pages);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/v1/content/portal/publish", HttpMethod.Post, new { version = Version(saved), configuration = draft })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PutAsJsonAsync("/api/v1/content/portal", new { version = Version(published), configuration = draft })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(staff, $"/api/v1/admin/portal-access/{grant}", HttpMethod.Delete)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await member.GetAsync("/api/v1/portal/configuration")).StatusCode);
    }

    [Fact]
    public async Task Portal_configuration_rejects_invalid_navigation_colours_and_reserved_pages()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app);
        var d = PortalPresentation.Default;
        foreach (var invalid in new[] {
            d with { BrandColor = "#ffffff" }, d with { DashboardSections = ["unknown"] },
            d with { Menu = d.Menu.Select(m => m with { Visible = false }).ToArray() },
            d with { Menu = [d.Menu[0], d.Menu[0]] },
            d with { Pages = [new("profile", "Overwrite", "Bad")] },
            d with { Pages = [new("../escape", "Escape", "Bad")] },
            d with { Name = "" }
        }) Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/portal", HttpMethod.Put, new ContentModule.PortalConfigurationRequest(null, invalid))).StatusCode);
    }

    [Fact]
    public async Task Portal_configuration_is_tenant_scoped_and_editors_cannot_publish_without_permission()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outsider = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync("/api/v1/content/portal")).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var system = db.BeginSystemAccess();
            var role = await db.Roles.SingleAsync(r => r.Name == "Outside Administrator");
            db.RolePermissions.AddRange(new RolePermission { RoleId = role.Id, Permission = "content.read" }, new RolePermission { RoleId = role.Id, Permission = "content.manage" });
            await db.SaveChangesAsync();
        }
        using var editor = await Login(app, "outside@nexora.test");
        await Json(await Send(staff, "/api/v1/content/portal", HttpMethod.Put, new ContentModule.PortalConfigurationRequest(null, PortalPresentation.Default with { Name = "Private tenant draft" })));
        var other = await Json(await editor.GetAsync("/api/v1/content/portal"));
        Assert.Equal("Member portal", other["draft"]!["name"]!.GetValue<string>()); Assert.Null(other["version"]);
        var saved = await Json(await Send(editor, "/api/v1/content/portal", HttpMethod.Put, new ContentModule.PortalConfigurationRequest(null, PortalPresentation.Default)));
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, "/api/v1/content/portal/publish", HttpMethod.Post, new { version = Version(saved), configuration = PortalPresentation.Default })).StatusCode);
    }
}
