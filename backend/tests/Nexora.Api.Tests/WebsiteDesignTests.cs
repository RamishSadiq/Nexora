using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Content;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using static Nexora.Api.Tests.WorkflowTestClient;

namespace Nexora.Api.Tests;

public sealed class WebsiteDesignTests
{
    [Fact]
    public async Task Design_drafts_are_private_and_publishing_requires_current_version_and_permission()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var visitor = app.CreateClient();
        var site = await Json(await Send(staff, "/api/v1/content/site", HttpMethod.Post, new { name = "Design association", tagline = "Together" }));
        var created = await Json(await Send(staff, "/api/v1/content/pages", HttpMethod.Post, new { slug = "home", title = "Our community", description = "", blocks = new[] { new { type = "hero", heading = "Welcome", text = "Join us" } } }));
        await Json(await Send(staff, "/api/v1/content/pages/" + Id(created["page"]!) + "/publish", HttpMethod.Post, new { version = Version(created["page"]!) }));
        var path = "/api/v1/public/sites/" + Id(site) + "/home";
        var design = new WebsiteDesign { AccentColor = "#aa7799", SectionGap = 48, Motion = new WebsiteMotion("slide", "load", 900, 100, 40, "linear", "glow") };
        var draft = await Json(await Send(staff, "/api/v1/content/site/design", HttpMethod.Put, new { version = (string?)null, configuration = design }));
        Assert.Equal("#aa7799", draft["draft"]!["accentColor"]!.GetValue<string>());
        Assert.True(draft["hasUnpublishedChanges"]!.GetValue<bool>());
        Assert.Equal("#c56586", (await Json(await visitor.GetAsync(path)))["design"]!["accentColor"]!.GetValue<string>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/v1/content/site/design", HttpMethod.Put, new { version = (string?)null, configuration = design })).StatusCode);
        await Json(await Send(staff, "/api/v1/content/site/design/publish", HttpMethod.Post, new { version = Version(draft) }));
        var visible = await Json(await visitor.GetAsync(path));
        Assert.Equal("#aa7799", visible["design"]!["accentColor"]!.GetValue<string>());
        Assert.Equal(900, visible["design"]!["motion"]!["durationMs"]!.GetValue<int>());
        Assert.Null(visible["draft"]); Assert.Null(visible["design"]!["tenantId"]);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(staff, "/api/v1/content/site/design/publish", HttpMethod.Post, new { version = Version(draft) })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>(); using var access = identity.BeginSystemAccess();
            // System administrators always have every permission; use an ordinary editor role.
            (await identity.Roles.SingleAsync(r => r.Name == "Platform Administrator")).IsSystem = false;
            identity.RolePermissions.RemoveRange(await identity.RolePermissions.Where(p => p.Permission == "content.publish").ToListAsync()); await identity.SaveChangesAsync();
        }
        using var editorOnly = await Login(app);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(editorOnly, "/api/v1/content/site/design/publish", HttpMethod.Post, new { version = Version(draft) })).StatusCode);
        using var outside = await Login(app, "outside@nexora.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(outside, "/api/v1/content/site/design", HttpMethod.Put, new { version = (string?)null, configuration = design })).StatusCode);
    }

    [Fact]
    public async Task Unsafe_design_links_motion_bounds_and_unreadable_colours_are_rejected()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app);
        await Json(await Send(staff, "/api/v1/content/site", HttpMethod.Post, new { name = "Design association", tagline = "" }));
        foreach (var design in new[] { new WebsiteDesign { PortalUrl = "javascript:alert(1)" }, new WebsiteDesign { BackgroundColor = "#ffffff", TextColor = "#ffffff" }, new WebsiteDesign { AccentColor = "red;display:none" }, new WebsiteDesign { Motion = new WebsiteMotion(DurationMs: 99999) }, new WebsiteDesign { Theme = "arbitrary-code" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/site/design", HttpMethod.Put, new { version = (string?)null, configuration = design })).StatusCode);
        var unsafeSection = new ContentBlock("showcase", "Our members", "", Items: [new ContentItem("Unsafe", "", LinkLabel: "Click", LinkUrl: "//outside.example")]);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/pages", HttpMethod.Post, new { slug = "home", title = "Home", description = "", blocks = new[] { unsafeSection } })).StatusCode);
        var invalidMotion = new ContentBlock("text", "Welcome", "", Motion: new WebsiteMotion(Distance: -1));
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(staff, "/api/v1/content/pages", HttpMethod.Post, new { slug = "home", title = "Home", description = "", blocks = new[] { invalidMotion } })).StatusCode);
    }

    [Fact]
    public async Task Section_layout_items_and_motion_follow_page_revision_publication()
    {
        using var app = new IdentityApiFactory(); using var staff = await Login(app); using var visitor = app.CreateClient();
        var site = await Json(await Send(staff, "/api/v1/content/site", HttpMethod.Post, new { name = "Design association", tagline = "" }));
        var block = new ContentBlock("showcase", "Explore membership", "Choose a topic", Layout: new ContentLayout("full", "center", "glass", "spacious", "Welcome"), Motion: new WebsiteMotion("fade", DurationMs: 450), Items: [new ContentItem("Individuals", "Individual membership"), new ContentItem("Organisations", "Organisation membership", LinkLabel: "Member portal", LinkUrl: "/portal")]);
        var created = await Json(await Send(staff, "/api/v1/content/pages", HttpMethod.Post, new { slug = "home", title = "Home", description = "", blocks = new[] { block } }));
        var page = await Json(await Send(staff, "/api/v1/content/pages/" + Id(created["page"]!) + "/publish", HttpMethod.Post, new { version = Version(created["page"]!) }));
        var publicPath = "/api/v1/public/sites/" + Id(site) + "/home";
        var visible = (await Json(await visitor.GetAsync(publicPath)))["page"]!["blocks"]![0]!;
        Assert.Equal("glass", visible["layout"]!["surface"]!.GetValue<string>()); Assert.Equal(450, visible["motion"]!["durationMs"]!.GetValue<int>()); Assert.Equal(2, visible["items"]!.AsArray().Count);
        var draft = await Json(await Send(staff, "/api/v1/content/pages/" + Id(page), HttpMethod.Put, new { slug = "home", title = "Home", description = "", version = Version(page), blocks = new[] { block with { Layout = block.Layout! with { Hidden = true } } } }));
        Assert.Single((await Json(await visitor.GetAsync(publicPath)))["page"]!["blocks"]!.AsArray());
        await Json(await Send(staff, "/api/v1/content/pages/" + Id(page) + "/publish", HttpMethod.Post, new { version = Version(draft["page"]!) }));
        Assert.Empty((await Json(await visitor.GetAsync(publicPath)))["page"]!["blocks"]!.AsArray());
    }
}
