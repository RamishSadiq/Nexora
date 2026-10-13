namespace Nexora.BuildingBlocks.Security;

public sealed record PortalMenuItem(string Key, string Label, bool Visible);
public sealed record PortalInfoPage(string Slug, string Title, string Body);
public sealed record PortalDesign
{
    public string Theme { get; init; } = "classic";
    public string BackgroundColor { get; init; } = "#09090b";
    public string TextColor { get; init; } = "#fafafa";
    public string AccentColor { get; init; } = "#50f5d6";
    public string SecondaryAccentColor { get; init; } = "#9255ff";
    public int CornerRadius { get; init; } = 24;
    public int GlowStrength { get; init; } = 30;
    public bool MotionEnabled { get; init; } = true;
    public int MotionDurationMs { get; init; } = 220;
}
public sealed record PortalPresentation(string Name, string Tagline, string WelcomeText, string BrandColor,
    PortalMenuItem[] Menu, string[] DashboardSections, PortalInfoPage[] Pages, PortalDesign? Design = null, PortalOnboarding? Onboarding = null)
{
    public static PortalPresentation Default => new("Member portal", "Your community. Your space.",
        "Here is your membership at a glance.", "#183a31",
        [new("dashboard", "Dashboard", true), new("memberships", "My memberships", true),
         new("applications", "My applications", true), new("organisations", "My organisations", true),
         new("profile", "My profile", true), new("notifications", "Notifications", true)],
        ["summary", "membership", "applications", "links"], []);
}
public interface IPortalPresentationDirectory
{
    Task<PortalPresentation> ReadAsync(CancellationToken ct);
}
