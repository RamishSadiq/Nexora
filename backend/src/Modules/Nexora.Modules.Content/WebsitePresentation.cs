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

public sealed class WebsiteConfiguration : TenantEntity
{
    public Guid SiteId { get; set; }
    public string DraftJson { get; set; } = "";
    public string? PublishedJson { get; set; }
}

public sealed record WebsiteMotion(string Effect = "rise", string Trigger = "enter", int DurationMs = 700, int DelayMs = 0,
    int Distance = 28, string Easing = "ease-out", string Hover = "lift");

public sealed record WebsiteDesign
{
    public string Theme { get; init; } = "studio";
    public string BackgroundColor { get; init; } = "#09090b";
    public string TextColor { get; init; } = "#fafafa";
    public string AccentColor { get; init; } = "#c56586";
    public string SecondaryAccentColor { get; init; } = "#9255ff";
    public string Font { get; init; } = "sans";
    public string HeadingSize { get; init; } = "large";
    public int ContentWidth { get; init; } = 1200;
    public int SectionGap { get; init; } = 32;
    public int CornerRadius { get; init; } = 24;
    public bool StickyHeader { get; init; } = true;
    public bool ShowTagline { get; init; } = true;
    public bool ShowPortalLink { get; init; } = true;
    public string PortalLabel { get; init; } = "Member portal";
    public string PortalUrl { get; init; } = "/portal";
    public bool ShowFooter { get; init; } = true;
    public string FooterText { get; init; } = "";
    public bool MotionEnabled { get; init; } = true;
    public WebsiteMotion Motion { get; init; } = new();
    public string AmbientEffect { get; init; } = "glow";
    public int AmbientDurationMs { get; init; } = 8000;
    public bool PauseAmbientOnHover { get; init; } = true;
}

public static partial class ContentModule
{
    public sealed record WebsiteConfigurationRequest(Guid? Version, WebsiteDesign Configuration);
    private static WebsiteDesign ReadWebsiteDesign(string? json) => string.IsNullOrEmpty(json) ? new() : JsonSerializer.Deserialize<WebsiteDesign>(json, Json)!;
    private static object ReadWebsiteConfiguration(WebsiteConfiguration? row) => new
    {
        version = row?.Version, draft = ReadWebsiteDesign(row?.DraftJson), published = ReadWebsiteDesign(row?.PublishedJson),
        hasUnpublishedChanges = row != null && row.DraftJson != row.PublishedJson
    };
    private static string? WebsiteLink(string? value)
    {
        var url = value?.Trim();
        if (string.IsNullOrEmpty(url)) return null;
        if (url.Length > 2000 || url.Any(char.IsControl) || url.Contains('\\') ||
            !(url.StartsWith('/') && !url.StartsWith("//")) &&
            !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo)))
            throw new ValidationException("Links must be local paths or HTTPS addresses.");
        return url;
    }
    private static void Choice(string? value, params string[] choices)
    {
        if (value == null || !choices.Contains(value)) throw new ValidationException("Unsupported website design option.");
    }
    private static void Range(int value, int min, int max)
    {
        if (value < min || value > max) throw new ValidationException($"Website setting must be between {min} and {max}.");
    }
    private static double Luminance(string? color)
    {
        if (!Regex.IsMatch(color ?? "", "^#[0-9a-fA-F]{6}$")) throw new ValidationException("Choose a six-digit website colour.");
        double Channel(int offset) { var c = Convert.ToInt32(color!.Substring(offset, 2), 16) / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
        return .2126 * Channel(1) + .7152 * Channel(3) + .0722 * Channel(5);
    }
    private static WebsiteMotion ValidateMotion(WebsiteMotion value)
    {
        if (value == null) throw new ValidationException("Choose website motion settings.");
        Choice(value.Effect, "none", "fade", "rise", "slide", "scale", "blur");
        Choice(value.Trigger, "enter", "load"); Choice(value.Easing, "ease", "ease-in", "ease-out", "ease-in-out", "linear");
        Choice(value.Hover, "none", "lift", "glow");
        Range(value.DurationMs, 100, 2000); Range(value.DelayMs, 0, 2000); Range(value.Distance, 0, 100);
        return value;
    }
    private static WebsiteDesign ValidateWebsiteDesign(WebsiteDesign value)
    {
        if (value == null) throw new ValidationException("Website configuration is required.");
        Choice(value.Theme, "studio", "classic"); Choice(value.Font, "sans", "serif", "mono"); Choice(value.HeadingSize, "small", "medium", "large");
        var background = Luminance(value.BackgroundColor); var foreground = Luminance(value.TextColor); Luminance(value.AccentColor); Luminance(value.SecondaryAccentColor);
        if ((Math.Max(background, foreground) + .05) / (Math.Min(background, foreground) + .05) < 4.5)
            throw new ValidationException("Website text and background must have at least 4.5:1 contrast.");
        Range(value.ContentWidth, 900, 1600); Range(value.SectionGap, 12, 120); Range(value.CornerRadius, 0, 48);
        ValidateMotion(value.Motion); Choice(value.AmbientEffect, "none", "float", "glow"); Range(value.AmbientDurationMs, 2000, 30000);
        var url = WebsiteLink(value.PortalUrl);
        if (value.ShowPortalLink && url == null) throw new ValidationException("Enter a header button address.");
        return value with { PortalLabel = Text(value.PortalLabel, 80, value.ShowPortalLink), PortalUrl = url ?? "", FooterText = Text(value.FooterText, 500, false) };
    }
    private static ContentLayout ValidateLayout(ContentLayout value)
    {
        Choice(value.Span, "full", "half", "third"); Choice(value.Alignment, "left", "center");
        Choice(value.Surface, "plain", "card", "accent", "glass"); Choice(value.Spacing, "compact", "normal", "spacious");
        return value with { Eyebrow = Text(value.Eyebrow, 80, false) };
    }
    private static void MapWebsitePresentation(RouteGroupBuilder api)
    {
        api.MapPut("/site/design", async (WebsiteConfigurationRequest request, ContentDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleOrDefaultAsync(ct) ?? throw new ValidationException("Create your website first.");
            var design = ValidateWebsiteDesign(request.Configuration);
            var row = await db.Set<WebsiteConfiguration>().SingleOrDefaultAsync(x => x.SiteId == site.Id, ct);
            if (row?.Version != request.Version) return Conflict();
            if (row == null) { row = new WebsiteConfiguration { TenantId = who.TenantId!.Value, SiteId = site.Id }; db.Add(row); }
            row.DraftJson = JsonSerializer.Serialize(design, Json);
            await db.SaveChangesAsync(ct); return Results.Ok(ReadWebsiteConfiguration(row));
        }).RequireAuthorization("content.manage");
        api.MapPost("/site/design/publish", async (PublishRequest request, ContentDbContext db, CancellationToken ct) =>
        {
            var row = await db.Set<WebsiteConfiguration>().SingleOrDefaultAsync(ct) ?? throw new ValidationException("Save a website design draft first.");
            if (row.Version != request.Version) return Conflict();
            ValidateWebsiteDesign(ReadWebsiteDesign(row.DraftJson)); row.PublishedJson = row.DraftJson;
            await db.SaveChangesAsync(ct); return Results.Ok(ReadWebsiteConfiguration(row));
        }).RequireAuthorization("content.publish");
    }
}
