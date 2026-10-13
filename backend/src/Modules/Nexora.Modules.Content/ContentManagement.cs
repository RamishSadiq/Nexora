using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Documents;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;

public static partial class ContentModule
{
    public sealed record BrandingRequest(Guid Version, string Name, string Tagline, string BrandColor, Guid? LogoMediaId);
    public sealed record NavigationRequest(Guid Version, string Label, int Order, bool Visible);
    public sealed record RenameRequest(Guid Version, string Slug);
    public sealed record MediaGrantRequest(Guid Version, bool IsPublic);
    public sealed record ScheduleRequest(Guid Version, DateTime PublishAtUtc, Guid RevisionId);
    private static object ReadMedia(MediaAsset m) => new { m.Id, m.Name, m.ScanStatus, m.IsPublic, m.Version, m.ContentType, size = m.Content.Length };
    private static async Task ValidateMedia(ContentDbContext db, Guid siteId, IEnumerable<Guid> ids, bool publishing, CancellationToken ct)
    {
        foreach (var id in ids.Distinct())
            if (!await db.Set<MediaAsset>().AnyAsync(m => m.Id == id && m.SiteId == siteId && (!publishing || m.IsPublic && m.ScanStatus == "clean"), ct))
                throw new ValidationException(publishing ? "Every image must pass its scan and have a public grant before publication." : "Choose an image from this website's media library.");
    }
    internal static IEnumerable<Guid> MediaIds(PageVersion revision) => (JsonSerializer.Deserialize<ContentBlock[]>(revision.BlocksJson, Json) ?? [])
        .SelectMany(b => (b.MediaId.HasValue ? new[] { b.MediaId.Value } : []).Concat((b.Items ?? []).Where(i => i.MediaId.HasValue).Select(i => i.MediaId!.Value)));
    private static async Task ValidateRevisionMedia(ContentDbContext db, Guid siteId, PageVersion revision, bool publishing, CancellationToken ct)
    {
        await ValidateMedia(db, siteId, MediaIds(revision), publishing, ct);
        foreach (var block in JsonSerializer.Deserialize<ContentBlock[]>(revision.BlocksJson, Json) ?? [])
            foreach (var id in (block.Type != "download" && block.MediaId.HasValue ? new[] { block.MediaId.Value } : []).Concat((block.Items ?? []).Where(i => i.MediaId.HasValue).Select(i => i.MediaId!.Value)))
            if (!await db.Set<MediaAsset>().AnyAsync(m => m.Id == id && m.SiteId == siteId && (m.ContentType == "image/png" || m.ContentType == "image/jpeg"), ct))
                throw new ValidationException("Image blocks require a PNG or JPEG. Use a download block for documents.");
    }
    private static async Task CancelSchedule(ContentDbContext db, Guid pageId, CancellationToken ct)
    {
        foreach (var job in await db.Set<ScheduledPublication>().Where(j => j.PageId == pageId && j.Status == "pending").ToListAsync(ct)) job.Status = "cancelled";
    }
    private static void MapManagement(RouteGroupBuilder api)
    {
        api.MapPut("/site", async (BrandingRequest r, ContentDbContext db, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleAsync(ct); if (site.Version != r.Version) return Conflict();
            if (!Regex.IsMatch(r.BrandColor ?? "", "^#[0-9a-fA-F]{6}$")) throw new ValidationException("Choose a six-digit brand colour.");
            double Channel(int offset) { var c = Convert.ToInt32(r.BrandColor!.Substring(offset, 2), 16) / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
            if (.2126 * Channel(1) + .7152 * Channel(3) + .0722 * Channel(5) > .16) throw new ValidationException("Choose a darker brand colour so white text stays readable.");
            if (r.LogoMediaId is Guid logo)
            {
                await ValidateMedia(db, site.Id, [logo], true, ct);
                if (!await db.Set<MediaAsset>().AnyAsync(m => m.Id == logo && (m.ContentType == "image/png" || m.ContentType == "image/jpeg"), ct)) throw new ValidationException("The logo must be a PNG or JPEG image.");
            }
            site.Name = Text(r.Name, 100); site.Tagline = Text(r.Tagline, 200, false); site.BrandColor = r.BrandColor!; site.LogoMediaId = r.LogoMediaId;
            await db.SaveChangesAsync(ct); return Results.Ok(site);
        }).RequireAuthorization("content.publish");
        api.MapPut("/pages/{id:guid}/navigation", async (Guid id, NavigationRequest r, ContentDbContext db, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct); if (page.Version != r.Version) return Conflict();
            if (r.Order is < 0 or > 999) throw new ValidationException("Menu position must be between 0 and 999.");
            page.NavigationLabel = Text(r.Label, 80, false); page.NavigationOrder = r.Order; page.ShowInNavigation = r.Visible;
            await db.SaveChangesAsync(ct); return Results.Ok(page);
        }).RequireAuthorization("content.publish");
        api.MapPut("/pages/{id:guid}/address", async (Guid id, RenameRequest r, ContentDbContext db, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct); if (page.Version != r.Version) return Conflict();
            var slug = Slug(r.Slug); if (page.Slug == "home" || slug == "home") throw new ValidationException("The home page address is reserved.");
            var site = await db.Sites.SingleAsync(s => s.Id == page.SiteId, ct);
            if (await db.Pages.AnyAsync(p => p.Slug == slug, ct) || await db.Set<PageRedirect>().AnyAsync(p => p.Slug == slug, ct)) throw new ValidationException("That address is already used by a page or redirect.");
            db.Add(new PageRedirect { TenantId = page.TenantId, SiteId = site.Id, PageId = page.Id, Slug = page.Slug });
            page.Slug = slug; site.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct); return Results.Ok(page);
        }).RequireAuthorization("content.publish");
        api.MapGet("/media", async (ContentDbContext db, CancellationToken ct) => Results.Ok(await db.Set<MediaAsset>().OrderByDescending(m => m.CreatedAtUtc).Select(m => new { m.Id, m.Name, m.ScanStatus, m.IsPublic, m.Version, m.ContentType, size = m.Content.Length }).ToListAsync(ct)));
        api.MapGet("/media/{id:guid}", async (Guid id, ContentDbContext db, HttpContext http, CancellationToken ct) =>
        {
            var media = await db.Set<MediaAsset>().SingleOrDefaultAsync(m => m.Id == id && m.ScanStatus == "clean", ct) ?? throw new KeyNotFoundException();
            http.Response.Headers["X-Content-Type-Options"] = "nosniff"; http.Response.Headers.ContentSecurityPolicy = "sandbox";
            return media.ContentType.StartsWith("image/") ? Results.File(media.Content, media.ContentType) : DocumentSafety.Download(http, media.Name, media.Content);
        });
        api.MapDelete("/media/{id:guid}", async (Guid id, [Microsoft.AspNetCore.Mvc.FromBody] PublishRequest r, ContentDbContext db, CancellationToken ct) =>
        {
            var media = await db.Set<MediaAsset>().SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new KeyNotFoundException();
            if (media.Version != r.Version) return Conflict();
            if (media.IsPublic || await db.Sites.AnyAsync(s => s.LogoMediaId == id, ct) || (await db.Versions.ToListAsync(ct)).Any(v => MediaIds(v).Contains(id)))
                throw new ValidationException("Only private images unused by any saved page version or logo can be deleted. Revoke public access to hide a retained image.");
            db.Remove(media); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("content.manage");
        api.MapPost("/media", async (HttpContext h, ContentDbContext db, IDocumentScanner scanner, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleAsync(ct); var upload = await DocumentSafety.ReadAsync(h.Request, ct); var form = await h.Request.ReadFormAsync(ct);
            if (!Guid.TryParse(form["version"], out var version) || version != site.Version) return Conflict();
            var ext = Path.GetExtension(upload.Name).ToLowerInvariant();
            if (ext is not (".png" or ".jpg" or ".jpeg" or ".pdf" or ".txt")) throw new ValidationException("Choose a PNG, JPEG, PDF or UTF-8 text file.");
            if (await db.Set<MediaAsset>().CountAsync(ct) >= 100) throw new ValidationException("This website supports up to 100 images.");
            var media = new MediaAsset { TenantId = site.TenantId, SiteId = site.Id, Name = upload.Name, Content = upload.Content, ContentType = ext switch { ".png" => "image/png", ".pdf" => "application/pdf", ".txt" => "text/plain", _ => "image/jpeg" }, ScanStatus = await scanner.ScanAsync(upload.Content, ct) };
            site.UpdatedAtUtc = DateTime.UtcNow; db.Add(media); await db.SaveChangesAsync(ct); return Results.Ok(ReadMedia(media));
        }).RequireAuthorization("content.manage");
        api.MapPost("/media/{id:guid}/scan", async (Guid id, PublishRequest r, ContentDbContext db, IDocumentScanner scanner, CancellationToken ct) =>
        {
            var media = await db.Set<MediaAsset>().SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new KeyNotFoundException();
            if (media.Version != r.Version) return Conflict();
            if (media.ScanStatus != "quarantined") throw new ValidationException("Only quarantined images can be rescanned.");
            media.ScanStatus = await scanner.ScanAsync(media.Content, ct); await db.SaveChangesAsync(ct); return Results.Ok(ReadMedia(media));
        }).RequireAuthorization("content.manage");
        api.MapPut("/media/{id:guid}/publication", async (Guid id, MediaGrantRequest r, ContentDbContext db, CancellationToken ct) =>
        {
            var media = await db.Set<MediaAsset>().SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new KeyNotFoundException();
            if (media.Version != r.Version) return Conflict();
            if (r.IsPublic && media.ScanStatus != "clean") throw new ValidationException("The security scan must pass before an image becomes public.");
            media.IsPublic = r.IsPublic; await db.SaveChangesAsync(ct); return Results.Ok(ReadMedia(media));
        }).RequireAuthorization("content.publish");
        api.MapGet("/schedules", async (ContentDbContext db, CancellationToken ct) => Results.Ok(await db.Set<ScheduledPublication>().OrderByDescending(s => s.CreatedAtUtc).Take(100).ToListAsync(ct)));
        api.MapPost("/pages/{id:guid}/schedule", async (Guid id, ScheduleRequest r, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct); if (page.Version != r.Version) return Conflict();
            if (r.PublishAtUtc.Kind != DateTimeKind.Utc || r.PublishAtUtc <= DateTime.UtcNow || r.PublishAtUtc > DateTime.UtcNow.AddYears(1)) throw new ValidationException("Choose a future UTC time within one year.");
            if (await db.Set<ScheduledPublication>().AnyAsync(j => j.PageId == id && j.Status == "pending", ct)) throw new ValidationException("Cancel this page's existing schedule first.");
            var revision = await db.Versions.SingleOrDefaultAsync(v => v.Id == r.RevisionId && v.PageId == id, ct) ?? throw new KeyNotFoundException();
            await ValidateMedia(db, page.SiteId, MediaIds(revision), true, ct);
            var job = new ScheduledPublication { TenantId = page.TenantId, PageId = id, RevisionId = revision.Id, PublisherId = who.UserId!.Value, PublishAtUtc = r.PublishAtUtc };
            page.UpdatedAtUtc = DateTime.UtcNow; db.Add(job); await db.SaveChangesAsync(ct); return Results.Ok(job);
        }).RequireAuthorization("content.publish");
        api.MapPost("/pages/{id:guid}/cancel-schedule", async (Guid id, PublishRequest r, ContentDbContext db, CancellationToken ct) =>
        {
            var page = await Find(db, id, ct); if (page.Version != r.Version) return Conflict();
            await CancelSchedule(db, id, ct); page.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(page);
        }).RequireAuthorization("content.publish");
    }
}
