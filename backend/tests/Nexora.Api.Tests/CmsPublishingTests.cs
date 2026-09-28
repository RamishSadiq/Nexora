using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Documents;
using Nexora.Modules.Content;
using Nexora.Modules.Identity;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;
public sealed class CmsPublishingTests
{
    private sealed class CleanScanner : IDocumentScanner { public Task<string> ScanAsync(byte[] content, CancellationToken ct) => Task.FromResult("clean"); }
    private static object Draft(string slug, string title, string? version = null, string? mediaId = null) => new { slug, title, description = "Description", version,
        blocks = new[] { new { type = mediaId == null ? "text" : "image", heading = "Heading", text = "Alternative text", mediaId } } };
    private static async Task<JsonNode> Site(HttpClient staff) => await Json(await Send(staff, "/api/v1/content/site", HttpMethod.Post, new { name = "Association", tagline = "Together" }));
    private static async Task<JsonNode> Create(HttpClient staff, string slug = "home", string? media = null) => await Json(await Send(staff, "/api/v1/content/pages", HttpMethod.Post, Draft(slug, "First title", mediaId: media)));
    private static async Task<JsonNode> Publish(HttpClient staff, JsonNode page) => await Json(await Send(staff, $"/api/v1/content/pages/{Id(page)}/publish", HttpMethod.Post, new { version = Version(page) }));

    [Fact]
    public async Task Branding_navigation_redirects_sitemap_and_domain_resolution_use_published_tenant_content()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var visitor = app.CreateClient();
        var site = await Site(staff); var created = await Create(staff, "about"); var page = await Publish(staff, created["page"]!);
        site = (await Json(await staff.GetAsync("/api/v1/content/site")))["site"]!;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/site", HttpMethod.Put, new { version = Version(site), name = "New association", tagline = "New tagline", brandColor = "#ffffff" })).StatusCode);
        await Json(await Send(staff, "/api/v1/content/site", HttpMethod.Put, new { version = Version(site), name = "New association", tagline = "New tagline", brandColor = "#123456" }));
        page = await Json(await Send(staff, $"/api/v1/content/pages/{Id(page)}/navigation", HttpMethod.Put, new { version = Version(page), label = "About us", order = 3, visible = true }));
        var publicPath = $"/api/v1/public/sites/{Id(site)}";
        var content = await Json(await visitor.GetAsync(publicPath + "/about"));
        Assert.Equal("#123456", content["site"]!["brandColor"]!.GetValue<string>());
        Assert.Equal("About us", content["navigation"]![0]!["title"]!.GetValue<string>());
        page = await Json(await Send(staff, $"/api/v1/content/pages/{Id(page)}/address", HttpMethod.Put, new { version = Version(page), slug = "our-story" }));
        Assert.Equal("our-story", (await Json(await visitor.GetAsync(publicPath + "/about")))["redirectSlug"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/pages", HttpMethod.Post, Draft("about", "Collision"))).StatusCode);
        await Create(staff, "private-draft");
        var config = app.Services.GetRequiredService<IConfiguration>(); config["Content:Domains:0:Host"] = "association.test"; config["Content:Domains:0:SiteId"] = Id(site);
        visitor.DefaultRequestHeaders.Host = "association.test";
        Assert.Equal(Id(site), (await Json(await visitor.GetAsync("/api/v1/public/resolve-site")))["siteId"]!.GetValue<string>());
        Assert.Equal("https://association.test/our-story", (await Json(await visitor.GetAsync(publicPath + "/our-story")))["canonical"]!.GetValue<string>());
        var sitemap = await visitor.GetStringAsync(publicPath + "/sitemap.xml"); Assert.Contains("https://association.test/our-story", sitemap); Assert.DoesNotContain("private-draft", sitemap);
        visitor.DefaultRequestHeaders.Host = "attacker.test"; visitor.DefaultRequestHeaders.Add("X-Forwarded-Host", "association.test");
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicPath + "/sitemap.xml")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/v1/public/resolve-site")).StatusCode);
        await Json(await Send(staff, $"/api/v1/content/pages/{Id(page)}/unpublish", HttpMethod.Post, new { version = Version(page) }));
        visitor.DefaultRequestHeaders.Host = "localhost";
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicPath + "/about")).StatusCode);
    }

    [Fact]
    public async Task Images_require_clean_scan_and_public_grant_and_revocation_hides_published_references()
    {
        using var app = new IdentityApiFactory(new CleanScanner()); using var staff = await Login(app); using var visitor = app.CreateClient();
        var site = await Site(staff);
        var token = await staff.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(Version(site)), "version"); form.Add(new ByteArrayContent(new byte[] { 255, 216, 255, 0 }), "file", "logo.jpg");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/content/media") { Content = form }; request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        var media = await Json(await staff.SendAsync(request));
        var publicMedia = $"/api/v1/public/sites/{Id(site)}/media/{Id(media)}";
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicMedia)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync($"/api/v1/content/media/{Id(media)}")).StatusCode);
        var created = await Create(staff, media: Id(media)); var page = created["page"]!;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, $"/api/v1/content/pages/{Id(page)}/publish", HttpMethod.Post, new { version = Version(page) })).StatusCode);
        media = await Json(await Send(staff, $"/api/v1/content/media/{Id(media)}/publication", HttpMethod.Put, new { version = Version(media), isPublic = true }));
        await Publish(staff, page);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync(publicMedia)).StatusCode);
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await outside.GetAsync($"/api/v1/content/media/{Id(media)}")).StatusCode);
        media = await Json(await Send(staff, $"/api/v1/content/media/{Id(media)}/publication", HttpMethod.Put, new { version = Version(media), isPublic = false }));
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicMedia)).StatusCode);
        Assert.Empty((await Json(await visitor.GetAsync($"/api/v1/public/sites/{Id(site)}/home")))["page"]!["blocks"]!.AsArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, $"/api/v1/content/media/{Id(media)}", HttpMethod.Delete, new { version = Version(media) })).StatusCode);
    }


    [Fact]
    public async Task Public_documents_are_attachments_and_cannot_be_used_as_images_or_logos()
    {
        using var app = new IdentityApiFactory(new CleanScanner()); using var staff = await Login(app); using var visitor = app.CreateClient();
        var site = await Site(staff); var token = await staff.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(Version(site)), "version"); form.Add(new ByteArrayContent("%PDF-1.4 test"u8.ToArray()), "file", "guide.pdf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/content/media") { Content = form }; request.Headers.Add("X-CSRF-TOKEN", token!.Token);
        var media = await Json(await staff.SendAsync(request));
        media = await Json(await Send(staff, $"/api/v1/content/media/{Id(media)}/publication", HttpMethod.Put, new { version = Version(media), isPublic = true }));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/pages", HttpMethod.Post, Draft("home", "Invalid image", mediaId: Id(media)))).StatusCode);
        var download = await visitor.GetAsync($"/api/v1/public/sites/{Id(site)}/media/{Id(media)}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode); Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        site = (await Json(await staff.GetAsync("/api/v1/content/site")))["site"]!;
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/site", HttpMethod.Put, new { version = Version(site), name = "Association", tagline = "", brandColor = "#123456", logoMediaId = Id(media) })).StatusCode);
        var page = (await Json(await Send(staff, "/api/v1/content/pages", HttpMethod.Post, new { slug = "guide", title = "Guide", description = "", blocks = new[] { new { type = "download", heading = "Guide", text = "Read our guide", mediaId = Id(media) } } })))["page"]!;
        await Publish(staff, page);
        Assert.Single((await Json(await visitor.GetAsync($"/api/v1/public/sites/{Id(site)}/guide")))["page"]!["blocks"]!.AsArray());
    }

    [Fact]
    public async Task Scheduled_publication_pins_revision_and_rechecks_publisher_access()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var visitor = app.CreateClient();
        var site = await Site(staff); var created = await Create(staff); var page = created["page"]!; var id = Id(page); var due = DateTime.UtcNow.AddHours(1);
        await Json(await Send(staff, $"/api/v1/content/pages/{id}/schedule", HttpMethod.Post, new { version = Version(page), revisionId = Id(created["revision"]!), publishAtUtc = due }));
        page = (await Json(await staff.GetAsync($"/api/v1/content/pages/{id}")))["page"]!;
        await Json(await Send(staff, $"/api/v1/content/pages/{id}", HttpMethod.Put, Draft("home", "Later draft", Version(page))));
        using (var scope = app.Services.CreateScope()) await ContentScheduler.DispatchAsync(scope.ServiceProvider, due.AddMinutes(1), default);
        using (var scope = app.Services.CreateScope())
        {
            await ContentScheduler.DispatchAsync(scope.ServiceProvider, due.AddMinutes(2), default);
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ContentDbContext>().Set<Publication>().IgnoreQueryFilters().CountAsync());
        }
        Assert.Equal("First title", (await Json(await visitor.GetAsync($"/api/v1/public/sites/{Id(site)}/home")))["page"]!["title"]!.GetValue<string>());
        var detail = await Json(await staff.GetAsync($"/api/v1/content/pages/{id}")); page = detail["page"]!;
        await Json(await Send(staff, $"/api/v1/content/pages/{id}/schedule", HttpMethod.Post, new { version = Version(page), revisionId = Id(detail["revision"]!), publishAtUtc = due }));
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var access = db.BeginSystemAccess();
            db.RolePermissions.RemoveRange(await db.RolePermissions.Where(p => p.Permission == "content.publish").ToListAsync()); await db.SaveChangesAsync();
            await ContentScheduler.DispatchAsync(scope.ServiceProvider, due.AddMinutes(1), default);
        }
        Assert.Equal("First title", (await Json(await visitor.GetAsync($"/api/v1/public/sites/{Id(site)}/home")))["page"]!["title"]!.GetValue<string>());
        var jobs = await Json(await staff.GetAsync("/api/v1/content/schedules")); Assert.Contains(jobs.AsArray(), j => j!["status"]!.GetValue<string>() == "failed");
    }
}
