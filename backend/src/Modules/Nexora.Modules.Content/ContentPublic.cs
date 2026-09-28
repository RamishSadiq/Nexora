using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;
public static partial class ContentModule
{
    private static bool Domain(IConfiguration config, string host, Guid siteId) => config.GetSection("Content:Domains").GetChildren()
        .Any(d => string.Equals(d["Host"], host, StringComparison.OrdinalIgnoreCase) && Guid.TryParse(d["SiteId"], out var id) && id == siteId);
    private static async Task<Site?> PublicSite(Guid siteId, ContentDbContext db, IPublicTenantDirectory tenants, IConfiguration config, IHostEnvironment env, HttpContext http, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var host = http.Request.Host.Host;
        if (!(env.IsDevelopment() && host is "localhost" or "127.0.0.1" or "::1") && !Domain(config, host, siteId)) return null;
        var site = await db.Sites.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(s => s.Id == siteId, ct);
        return site != null && await tenants.IsActiveAsync(site.TenantId, ct) ? site : null;
    }
    private static void MapPublic(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/public/resolve-site", async (ContentDbContext db, IPublicTenantDirectory tenants, IConfiguration config, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            var match = config.GetSection("Content:Domains").GetChildren().SingleOrDefault(d => string.Equals(d["Host"], http.Request.Host.Host, StringComparison.OrdinalIgnoreCase));
            if (match == null || !Guid.TryParse(match["SiteId"], out var id)) return Results.NotFound();
            var site = await db.Sites.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.Id == id, ct);
            return site != null && await tenants.IsActiveAsync(site.TenantId, ct) ? Results.Ok(new { siteId = id }) : Results.NotFound();
        }).AllowAnonymous();
        endpoints.MapGet("/api/v1/public/sites/{siteId:guid}/media/{id:guid}", async (Guid siteId, Guid id, ContentDbContext db, IPublicTenantDirectory tenants, IConfiguration config, IHostEnvironment env, HttpContext http, CancellationToken ct) =>
        {
            var site = await PublicSite(siteId, db, tenants, config, env, http, ct); if (site == null) return Results.NotFound();
            var media = await db.Set<MediaAsset>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(m => m.Id == id && m.SiteId == siteId && m.TenantId == site.TenantId && m.IsPublic && m.ScanStatus == "clean", ct);
            if (media == null) return Results.NotFound();
            http.Response.Headers["X-Content-Type-Options"] = "nosniff"; http.Response.Headers.ContentSecurityPolicy = "sandbox";
            return media.ContentType.StartsWith("image/") ? Results.File(media.Content, media.ContentType) : Nexora.BuildingBlocks.Documents.DocumentSafety.Download(http, media.Name, media.Content);
        }).AllowAnonymous();
        endpoints.MapGet("/api/v1/public/sites/{siteId:guid}/sitemap.xml", async (Guid siteId, ContentDbContext db, IPublicTenantDirectory tenants, IConfiguration config, IHostEnvironment env, HttpContext http, CancellationToken ct) =>
        {
            var site = await PublicSite(siteId, db, tenants, config, env, http, ct); if (site == null) return Results.NotFound();
            var root = Domain(config, http.Request.Host.Host, siteId);
            var origin = root ? "https://" + http.Request.Host.Host : $"{http.Request.Scheme}://{http.Request.Host}/site/{siteId}";
            var slugs = await db.Pages.IgnoreQueryFilters().Where(p => p.SiteId == siteId && p.TenantId == site.TenantId && p.PublishedId != null).OrderBy(p => p.Slug).Select(p => p.Slug).ToListAsync(ct);
            var xml = new System.Xml.Linq.XDocument(new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                slugs.Select(slug => new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                    new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9"), origin + (root && slug == "home" ? "/" : "/" + slug))))));
            return Results.Text(xml.ToString(), "application/xml");
        }).AllowAnonymous();
        endpoints.MapGet("/api/v1/public/sites/{siteId:guid}/{slug}", async (Guid siteId, string slug, ContentDbContext db, IPublicTenantDirectory tenants, IConfiguration config, IHostEnvironment env, HttpContext http, CancellationToken ct) =>
        {
            var site = await PublicSite(siteId, db, tenants, config, env, http, ct); if (site == null) return Results.NotFound();
            var published = await (from p in db.Pages.IgnoreQueryFilters().AsNoTracking()
                join v in db.Versions.IgnoreQueryFilters().AsNoTracking() on p.PublishedId equals v.Id
                where p.SiteId == siteId && p.TenantId == site.TenantId && v.TenantId == site.TenantId && v.PageId == p.Id
                select new { Page = p, Revision = v }).ToListAsync(ct);
            var result = published.SingleOrDefault(p => p.Page.Slug == slug);
            if (result == null)
            {
                var alias = await db.Set<PageRedirect>().IgnoreQueryFilters().SingleOrDefaultAsync(r => r.SiteId == siteId && r.TenantId == site.TenantId && r.Slug == slug, ct);
                var target = published.SingleOrDefault(p => p.Page.Id == alias?.PageId);
                return target == null ? Results.NotFound() : Results.Ok(new { redirectSlug = target.Page.Slug, basePath = Domain(config, http.Request.Host.Host, siteId) ? "" : $"/site/{siteId}" });
            }
            var publicMedia = await db.Set<MediaAsset>().IgnoreQueryFilters().Where(m => m.SiteId == siteId && m.TenantId == site.TenantId && m.IsPublic && m.ScanStatus == "clean").Select(m => m.Id).ToListAsync(ct);
            var blocks = (JsonSerializer.Deserialize<ContentBlock[]>(result.Revision.BlocksJson, Json) ?? []).Where(b => b.Type is not ("image" or "download") || b.MediaId.HasValue && publicMedia.Contains(b.MediaId.Value));
            var root = Domain(config, http.Request.Host.Host, siteId); var basePath = root ? "" : $"/site/{siteId}";
            var canonical = root ? $"https://{http.Request.Host.Host}{(slug == "home" ? "/" : "/" + slug)}" : null;
            return Results.Ok(new { site = new { site.Id, site.Name, site.Tagline, site.BrandColor, logoMediaId = site.LogoMediaId.HasValue && publicMedia.Contains(site.LogoMediaId.Value) ? site.LogoMediaId : null }, slug, basePath, canonical,
                page = new { result.Revision.Id, result.Revision.Title, result.Revision.Description, result.Revision.CreatedAtUtc, blocks },
                navigation = published.Where(p => p.Page.ShowInNavigation).OrderBy(p => p.Page.NavigationOrder).ThenBy(p => p.Page.Slug).Select(p => new { p.Page.Slug, title = string.IsNullOrEmpty(p.Page.NavigationLabel) ? p.Revision.Title : p.Page.NavigationLabel }) });
        }).AllowAnonymous();
    }
}
