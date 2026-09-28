namespace Nexora.BuildingBlocks.Security;

public sealed record PortalMenuItem(string Key, string Label, bool Visible);
public sealed record PortalInfoPage(string Slug, string Title, string Body);
public sealed record PortalPresentation(string Name, string Tagline, string WelcomeText, string BrandColor,
    PortalMenuItem[] Menu, string[] DashboardSections, PortalInfoPage[] Pages)
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
