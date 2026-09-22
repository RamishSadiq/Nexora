using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;

public static class ContentModule
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static IServiceCollection AddNexoraContent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContentDbContext>(o => o.UseSqlServer(configuration.GetConnectionString("Nexora"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "content")));
        return services;
    }
    public static void MapNexoraContent(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1/content").RequireAuthorization("content.read").WithTags("Content");
        api.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            try
            {
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method)) await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (AntiforgeryValidationException) { return Results.Problem(statusCode: 400, title: "Invalid security token."); }
            catch (ValidationException e) { return Results.Problem(statusCode: 400, title: e.Message); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (TenantBoundaryException) { return Results.NotFound(); }
            catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "This page changed. Reload before saving or publishing."); }
            catch (DbUpdateException) { return Results.Problem(statusCode: 409, title: "This site or page address already exists, or the page changed. Reload and try again."); }
        });
        api.MapGet("/site", async (ContentDbContext db, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleOrDefaultAsync(ct);
            var pages = await (from p in db.Pages join v in db.Versions on p.DraftId equals v.Id orderby p.Slug select new { p.Id, p.Slug, p.Version, p.DraftId, p.PublishedId, v.Title }).ToListAsync(ct);
            return Results.Ok(new { site, pages });
        });
        api.MapPost("/site", async (SiteRequest r, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            if (await db.Sites.AnyAsync(ct)) return Results.Conflict();
            var site = new Site { TenantId = who.TenantId!.Value, Name = Text(r.Name, 100), Tagline = Text(r.Tagline, 200, false) };
            db.Add(site); await db.SaveChangesAsync(ct); return Results.Ok(site);
        }).RequireAuthorization("content.manage");
        api.MapPost("/pages", async (PageRequest r, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleOrDefaultAsync(ct) ?? throw new ValidationException("Create your website first.");
            if (await db.Pages.CountAsync(ct) >= 100) throw new ValidationException("This website supports up to 100 pages.");
            var page = new Page { TenantId = site.TenantId, SiteId = site.Id, Slug = Slug(r.Slug) };
            var revision = Revision(page, r, who.UserId!.Value); page.DraftId = revision.Id;
            db.Add(page); db.Add(revision); await db.SaveChangesAsync(ct); return Results.Ok(new { page, revision = ReadRevision(revision) });
        }).RequireAuthorization("content.manage");
        api.MapGet("/pages/{id:guid}", async (Guid id, ContentDbContext db, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct);
            var revision = await db.Versions.SingleAsync(v => v.Id == page.DraftId && v.PageId == id, ct);
            var history = await db.Versions.Where(v => v.PageId == id).OrderByDescending(v => v.CreatedAtUtc).Select(v => new { v.Id, v.Title, v.CreatedAtUtc }).Take(100).ToListAsync(ct);
            return Results.Ok(new { page, revision = ReadRevision(revision), history });
        });
        api.MapPut("/pages/{id:guid}", async (Guid id, PageRequest r, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct);
            if (page.Version != r.Version) return Conflict();
            if (page.Slug != r.Slug) throw new ValidationException("Page addresses are fixed in this first website release.");
            var revision = Revision(page, r, who.UserId!.Value); db.Add(revision); page.DraftId = revision.Id;
            await db.SaveChangesAsync(ct); return Results.Ok(new { page, revision = ReadRevision(revision) });
        }).RequireAuthorization("content.manage");
        api.MapPost("/pages/{id:guid}/publish", async (Guid id, PublishRequest r, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct); if (page.Version != r.Version) return Conflict();
            var revisionId = r.RevisionId ?? page.DraftId;
            if (!await db.Versions.AnyAsync(v => v.Id == revisionId && v.PageId == id, ct)) return Results.NotFound();
            page.PublishedId = revisionId;
            // Audit and published pointer commit atomically. Public reads are no-store in this first slice.
            db.Add(new Publication { TenantId = page.TenantId, PageId = id, PageVersionId = revisionId, ActorId = who.UserId!.Value });
            await db.SaveChangesAsync(ct); return Results.Ok(page);
        }).RequireAuthorization("content.publish");
        api.MapPost("/pages/{id:guid}/unpublish", async (Guid id, PublishRequest r, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct); if (page.Version != r.Version) return Conflict();
            page.PublishedId = null;
            db.Add(new Publication { TenantId = page.TenantId, PageId = id, ActorId = who.UserId!.Value });
            await db.SaveChangesAsync(ct); return Results.Ok(page);
        }).RequireAuthorization("content.publish");
        endpoints.MapGet("/api/v1/public/sites/{siteId:guid}/{slug}", async (Guid siteId, string slug, ContentDbContext db, IPublicTenantDirectory tenants, IConfiguration config, IHostEnvironment env, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            // Only configured host/site pairs are public in production. Forwarded host is deliberately ignored.
            var host = http.Request.Host.Host.ToLowerInvariant();
            var local = env.IsDevelopment() && host is "localhost" or "127.0.0.1" or "::1";
            if (!local && config.GetSection("Content:Domains").GetChildren().All(d => !string.Equals(d["Host"], host, StringComparison.OrdinalIgnoreCase) || d["SiteId"] != siteId.ToString())) return Results.NotFound();
            var site = await db.Sites.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(s => s.Id == siteId, ct);
            if (site == null || !await tenants.IsActiveAsync(site.TenantId, ct)) return Results.NotFound();
            var published = from p in db.Pages.IgnoreQueryFilters().AsNoTracking()
                            join v in db.Versions.IgnoreQueryFilters().AsNoTracking() on p.PublishedId equals v.Id
                            where p.SiteId == siteId && p.TenantId == site.TenantId && v.TenantId == site.TenantId && v.PageId == p.Id
                            select new { p.Slug, Revision = v };
            var result = await published.SingleOrDefaultAsync(p => p.Slug == slug, ct);
            if (result == null) return Results.NotFound();
            var navigation = await published.OrderBy(p => p.Slug == "home" ? 0 : 1).ThenBy(p => p.Slug).Select(p => new { p.Slug, p.Revision.Title }).ToListAsync(ct);
            return Results.Ok(new { site = new { site.Id, site.Name, site.Tagline }, slug, page = ReadRevision(result.Revision), navigation });
        }).AllowAnonymous().WithTags("Public content");
    }
    private static object ReadRevision(PageVersion r) => new { r.Id, r.Title, r.Description, r.CreatedAtUtc, blocks = JsonSerializer.Deserialize<ContentBlock[]>(r.BlocksJson, Json) };
    private static Task<Page> Find(ContentDbContext db, Guid id, CancellationToken ct) => FindCore(db, id, ct);
    private static async Task<Page> FindCore(ContentDbContext db, Guid id, CancellationToken ct) => await db.Pages.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new KeyNotFoundException();
    private static IResult Conflict() => Results.Problem(statusCode: 409, title: "This page changed. Reload before saving or publishing.");
    private static string Text(string? value, int max, bool required = true)
    {
        var text = value?.Trim() ?? "";
        if ((required && text.Length == 0) || text.Length > max) throw new ValidationException($"Text must be {(required ? "1" : "0")}–{max} characters.");
        return text;
    }
    private static string Slug(string value)
    {
        if (value == null || !Regex.IsMatch(value, "^[a-z0-9]+(?:-[a-z0-9]+)*$") || value.Length > 100) throw new ValidationException("Use a lowercase page address with letters, numbers and hyphens.");
        return value;
    }
    private static PageVersion Revision(Page page, PageRequest r, Guid author)
    {
        if (r.Blocks == null || r.Blocks.Length is < 1 or > 20) throw new ValidationException("Choose between 1 and 20 content blocks.");
        var blocks = r.Blocks.Select(b =>
        {
            if (b == null || b.Type is not ("hero" or "text" or "feature" or "cta")) throw new ValidationException("Unsupported content block.");
            var url = b.LinkUrl?.Trim();
            if (!string.IsNullOrEmpty(url) && !(url.StartsWith('/') && !url.StartsWith("//") && !url.Contains('\\') && !url.Any(char.IsControl)) &&
                !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo))) throw new ValidationException("Links must be local paths or HTTPS addresses.");
            if (url?.Length > 2000) throw new ValidationException("Link is too long.");
            return new ContentBlock(b.Type, Text(b.Heading, 160, false), Text(b.Text, 3000, false), Text(b.LinkLabel, 80, !string.IsNullOrEmpty(url)), url);
        }).ToArray();
        var json = JsonSerializer.Serialize(blocks, Json); if (json.Length > 40000) throw new ValidationException("Page content is too large.");
        return new PageVersion { TenantId = page.TenantId, PageId = page.Id, Title = Text(r.Title, 120), Description = Text(r.Description, 300, false), BlocksJson = json, AuthorId = author };
    }
}
