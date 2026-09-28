using Nexora.BuildingBlocks.Domain;

namespace Nexora.Modules.Content;

public sealed class Site : TenantEntity
{
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string BrandColor { get; set; } = "#183a31";
    public Guid? LogoMediaId { get; set; }
}
public sealed class Page : TenantEntity
{
    public Guid SiteId { get; set; }
    public string Slug { get; set; } = "";
    public Guid DraftId { get; set; }
    public Guid? PublishedId { get; set; }
    public int NavigationOrder { get; set; }
    public string NavigationLabel { get; set; } = "";
    public bool ShowInNavigation { get; set; } = true;
}
public sealed class MediaAsset : TenantEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public string ScanStatus { get; set; } = "quarantined";
    public bool IsPublic { get; set; }
}
public sealed class PageRedirect : TenantEntity
{
    public Guid SiteId { get; set; }
    public Guid PageId { get; set; }
    public string Slug { get; set; } = "";
}
public sealed class ScheduledPublication : TenantEntity
{
    public Guid PageId { get; set; }
    public Guid RevisionId { get; set; }
    public Guid PublisherId { get; set; }
    public DateTime PublishAtUtc { get; set; }
    public string Status { get; set; } = "pending";
    public string? Failure { get; set; }
}
public sealed class PageVersion : TenantEntity, IAppendOnly
{
    public Guid PageId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string BlocksJson { get; set; } = "[]";
    public Guid AuthorId { get; set; }
}
public sealed class Publication : TenantEntity, IAppendOnly
{
    public Guid PageId { get; set; }
    public Guid? PageVersionId { get; set; }
    public Guid ActorId { get; set; }
}
public sealed record ContentBlock(string Type, string Heading, string Text, string? LinkLabel = null, string? LinkUrl = null, Guid? MediaId = null);
public sealed record SiteRequest(string Name, string Tagline);
public sealed record PageRequest(string Slug, string Title, string Description, ContentBlock[] Blocks, Guid? Version);
public sealed record PublishRequest(Guid Version, Guid? RevisionId = null);
