using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;

public sealed class PortalConfiguration : TenantEntity
{
    public string DraftJson { get; set; } = "";
    public string? PublishedJson { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? PublishedBy { get; set; }
}
internal sealed class PortalPresentationDirectory(ContentDbContext db) : IPortalPresentationDirectory
{
    public async Task<PortalPresentation> ReadAsync(CancellationToken ct)
    {
        var json = await db.Set<PortalConfiguration>().Select(x => x.PublishedJson).SingleOrDefaultAsync(ct);
        return json == null ? PortalPresentation.Default : JsonSerializer.Deserialize<PortalPresentation>(json)!;
    }
}
public static partial class ContentModule
{
    public sealed record PortalConfigurationRequest(Guid? Version, PortalPresentation Configuration);
    private static object ReadPortalConfiguration(PortalConfiguration? row) => new
    {
        version = row?.Version,
        draft = row == null ? PortalPresentation.Default : JsonSerializer.Deserialize<PortalPresentation>(row.DraftJson),
        published = row?.PublishedJson == null ? PortalPresentation.Default : JsonSerializer.Deserialize<PortalPresentation>(row.PublishedJson),
        publishedAtUtc = row?.PublishedAtUtc,
        hasUnpublishedChanges = row != null && row.DraftJson != row.PublishedJson
    };
    private static void ValidatePortalPresentation(PortalPresentation value)
    {
        if (value == null) throw new ValidationException("Portal configuration is required.");
        Text(value.Name, 100); Text(value.Tagline, 200, false); Text(value.WelcomeText, 300, false);
        if (!Regex.IsMatch(value.BrandColor ?? "", "^#[0-9a-fA-F]{6}$")) throw new ValidationException("Choose a six-digit brand colour.");
        double Channel(int offset) { var c = Convert.ToInt32(value.BrandColor!.Substring(offset, 2), 16) / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
        if (.2126 * Channel(1) + .7152 * Channel(3) + .0722 * Channel(5) > .16) throw new ValidationException("Choose a darker brand colour so white text stays readable.");
        var design = value.Design ?? new PortalDesign();
        Choice(design.Theme, "classic", "neon");
        var background = Luminance(design.BackgroundColor); var foreground = Luminance(design.TextColor);
        Luminance(design.AccentColor); Luminance(design.SecondaryAccentColor);
        if ((Math.Max(background, foreground) + .05) / (Math.Min(background, foreground) + .05) < 4.5)
            throw new ValidationException("Portal text and background must have at least 4.5:1 contrast.");
        Range(design.CornerRadius, 0, 32); Range(design.GlowStrength, 0, 60); Range(design.MotionDurationMs, 100, 1000);
        if (value.Pages == null || value.Pages.Length > 30 || value.Menu == null || value.DashboardSections == null)
            throw new ValidationException("Provide navigation, dashboard sections and at most 30 custom pages.");
        var keys = PortalPresentation.Default.Menu.Select(m => m.Key).ToHashSet();
        foreach (var page in value.Pages)
        {
            if (page == null || !Regex.IsMatch(page.Slug ?? "", "^[a-z][a-z0-9-]{0,59}$") || !keys.Add(page.Slug!) || page.Slug is "api" or "login" or "logout" or "csrf" or "signup" or "register")
                throw new ValidationException("Use unique page addresses with letters, numbers and hyphens; built-in addresses are reserved.");
            Text(page.Title, 120); Text(page.Body, 10000, false);
        }
        if (value.Menu.Length != keys.Count || value.Menu.Any(m => m == null || !keys.Contains(m.Key)) || value.Menu.Select(m => m.Key).Distinct().Count() != keys.Count)
            throw new ValidationException("Include each built-in section and custom page exactly once in navigation.");
        foreach (var item in value.Menu) Text(item.Label, 80);
        if (!value.Menu.Single(m => m.Key == "dashboard").Visible) throw new ValidationException("Dashboard must remain visible.");
        var sections = PortalPresentation.Default.DashboardSections;
        if (value.DashboardSections.Length == 0 || value.DashboardSections.Distinct().Count() != value.DashboardSections.Length || value.DashboardSections.Any(s => !sections.Contains(s)))
            throw new ValidationException("Choose at least one dashboard section, without duplicates.");
        if (JsonSerializer.Serialize(value).Length > 100000) throw new ValidationException("Portal configuration is too large.");
    }
    private static void MapPortalPresentation(RouteGroupBuilder api)
    {
        api.MapGet("/portal", async (ContentDbContext db, CancellationToken ct) => Results.Ok(ReadPortalConfiguration(await db.Set<PortalConfiguration>().SingleOrDefaultAsync(ct))));
        api.MapPut("/portal", async (PortalConfigurationRequest request, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            ValidatePortalPresentation(request.Configuration);
            var row = await db.Set<PortalConfiguration>().SingleOrDefaultAsync(ct);
            if (row?.Version != request.Version) return Conflict();
            if (row == null) { row = new PortalConfiguration { TenantId = who.TenantId!.Value }; db.Add(row); }
            row.DraftJson = JsonSerializer.Serialize(request.Configuration);
            await db.SaveChangesAsync(ct); return Results.Ok(ReadPortalConfiguration(row));
        }).RequireAuthorization("content.manage");
        api.MapPost("/portal/publish", async (PortalConfigurationRequest request, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var row = await db.Set<PortalConfiguration>().SingleOrDefaultAsync(ct) ?? throw new ValidationException("Save a portal draft first.");
            if (row.Version != request.Version) return Conflict();
            ValidatePortalPresentation(JsonSerializer.Deserialize<PortalPresentation>(row.DraftJson)!);
            row.PublishedJson = row.DraftJson; row.PublishedAtUtc = DateTime.UtcNow; row.PublishedBy = who.UserId;
            await db.SaveChangesAsync(ct); return Results.Ok(ReadPortalConfiguration(row));
        }).RequireAuthorization("content.publish");
    }
}
