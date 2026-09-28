using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Api.Tests;

public sealed class ContentEndpointTests
{
    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, object body, HttpMethod? method = null)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        var request = new HttpRequestMessage(method ?? HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token); return await client.SendAsync(request);
    }
    private static async Task<HttpClient> Login(IdentityApiFactory app, string email = IdentityApiFactory.AdminEmail)
    {
        var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await Send(client, "/api/v1/auth/login", new LoginRequest(email, IdentityApiFactory.AdminPassword, false))).StatusCode);
        return client;
    }
    private static async Task<JsonNode> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, text);
        return JsonNode.Parse(text)!;
    }
    private static object Draft(string title = "Welcome", string? version = null, string url = "https://example.org") => new
    {
        slug = "home", title, description = "Association website", version,
        blocks = new[] { new { type = "hero", heading = "Connect", text = "Published text", linkLabel = "Learn more", linkUrl = url } }
    };

    [Fact]
    public async Task Content_editors_cannot_cross_tenants_or_publish_without_publisher_permission()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app);
        using (var scope = app.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
            using var bootstrap = identity.BeginSystemAccess();
            var role = await identity.Roles.SingleAsync(r => r.Name == "Outside Administrator");
            identity.RolePermissions.AddRange(new RolePermission { RoleId = role.Id, Permission = "content.read" }, new RolePermission { RoleId = role.Id, Permission = "content.manage" });
            await identity.SaveChangesAsync();
        }
        using var editor = await Login(app, "outside@nexora.test");
        await Json(await Send(staff, "/api/v1/content/site", new { name = "Tenant A", tagline = "" }));
        var page = await Json(await Send(staff, "/api/v1/content/pages", Draft()));
        var id = page["page"]!["id"]!.GetValue<string>(); var version = page["page"]!["version"]!.GetValue<string>();
        Assert.Null((await Json(await editor.GetAsync("/api/v1/content/site")))["site"]);
        Assert.Equal(HttpStatusCode.NotFound, (await editor.GetAsync("/api/v1/content/pages/" + id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(editor, "/api/v1/content/pages/" + id, Draft("Attack", version), HttpMethod.Put)).StatusCode);
        await Json(await Send(editor, "/api/v1/content/site", new { name = "Tenant B", tagline = "" }));
        var own = await Json(await Send(editor, "/api/v1/content/pages", Draft()));
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, $"/api/v1/content/pages/{own["page"]!["id"]!.GetValue<string>()}/publish", new { version = own["page"]!["version"]!.GetValue<string>() })).StatusCode);
        var ownPage = own["page"]!; var ownId = ownPage["id"]!.GetValue<string>(); var ownVersion = ownPage["version"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, $"/api/v1/content/pages/{ownId}/navigation", new { version = ownVersion, label = "Menu", order = 0, visible = true }, HttpMethod.Put)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, $"/api/v1/content/pages/{ownId}/address", new { version = ownVersion, slug = "changed" }, HttpMethod.Put)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editor, $"/api/v1/content/pages/{ownId}/schedule", new { version = ownVersion, revisionId = own["revision"]!["id"]!.GetValue<string>(), publishAtUtc = DateTime.UtcNow.AddHours(1) })).StatusCode);
        var forbiddenRevision = own["revision"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.NotFound, (await Send(staff, $"/api/v1/content/pages/{id}/publish", new { version, revisionId = forbiddenRevision })).StatusCode);
    }

    [Fact]
    public async Task Draft_publish_update_rollback_unpublish_and_navigation_are_isolated()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var anonymous = app.CreateClient();
        var site = await Json(await Send(staff, "/api/v1/content/site", new { name = "Association", tagline = "Together" }));
        var created = await Json(await Send(staff, "/api/v1/content/pages", Draft()));
        var id = created["page"]!["id"]!.GetValue<string>(); var path = "/api/v1/content/pages/" + id;
        var version = created["page"]!["version"]!.GetValue<string>(); var firstRevision = created["revision"]!["id"]!.GetValue<string>();
        var publicPath = $"/api/v1/public/sites/{site["id"]!.GetValue<string>()}/home";
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(publicPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        var published = await Json(await Send(staff, path + "/publish", new { version }));
        var visible = await Json(await anonymous.GetAsync(publicPath));
        Assert.Equal("Welcome", visible["page"]!["title"]!.GetValue<string>());
        Assert.Null(visible["site"]!["tenantId"]); Assert.Null(visible["page"]!["authorId"]);
        Assert.Single(visible["navigation"]!.AsArray());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, path, Draft("Stale", version), HttpMethod.Put)).StatusCode);
        var changed = await Json(await Send(staff, path, Draft("New draft", published["version"]!.GetValue<string>()), HttpMethod.Put));
        Assert.Equal("Welcome", (await Json(await anonymous.GetAsync(publicPath)))["page"]!["title"]!.GetValue<string>());
        var republished = await Json(await Send(staff, path + "/publish", new { version = changed["page"]!["version"]!.GetValue<string>() }));
        Assert.Equal("New draft", (await Json(await anonymous.GetAsync(publicPath)))["page"]!["title"]!.GetValue<string>());
        var restored = await Json(await Send(staff, path + "/publish", new { version = republished["version"]!.GetValue<string>(), revisionId = firstRevision }));
        Assert.Equal("Welcome", (await Json(await anonymous.GetAsync(publicPath)))["page"]!["title"]!.GetValue<string>());
        await Json(await Send(staff, path + "/unpublish", new { version = restored["version"]!.GetValue<string>() }));
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(publicPath)).StatusCode);
    }

    [Fact]
    public async Task Unsafe_content_unknown_hosts_and_suspended_tenants_are_rejected()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var outside = await Login(app, "outside@nexora.test");
        var site = await Json(await Send(staff, "/api/v1/content/site", new { name = "Safe association", tagline = "" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/pages", Draft(url: "javascript:alert(1)"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/pages", Draft(url: "//evil.example"))).StatusCode);
        var created = await Json(await Send(staff, "/api/v1/content/pages", Draft()));
        var path = "/api/v1/content/pages/" + created["page"]!["id"]!.GetValue<string>();
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync(path)).StatusCode);
        var published = await Json(await Send(staff, path + "/publish", new { version = created["page"]!["version"]!.GetValue<string>() }));
        using var anonymous = app.CreateClient();
        var publicPath = $"/api/v1/public/sites/{site["id"]!.GetValue<string>()}/home";
        using var spoof = new HttpRequestMessage(HttpMethod.Get, publicPath);
        spoof.Headers.Host = "unknown.example"; spoof.Headers.Add("X-Forwarded-Host", "localhost");
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.SendAsync(spoof)).StatusCode);
        var session = (await staff.GetFromJsonAsync<SessionResponse>("/api/v1/auth/session"))!;
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
        using var bootstrap = db.BeginSystemAccess();
        (await db.Tenants.SingleAsync(t => t.Id == session.TenantId)).Status = TenantStatus.Suspended; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(publicPath)).StatusCode);
    }
}
