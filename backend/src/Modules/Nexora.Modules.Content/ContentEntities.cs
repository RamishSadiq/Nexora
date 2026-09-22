using Nexora.BuildingBlocks.Domain;

namespace Nexora.Modules.Content;

public sealed class Site : TenantEntity
{
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
}
public sealed class Page : TenantEntity
{
    public Guid SiteId { get; set; }
    public string Slug { get; set; } = "";
    public Guid DraftId { get; set; }
    public Guid? PublishedId { get; set; }
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
public sealed record ContentBlock(string Type, string Heading, string Text, string? LinkLabel = null, string? LinkUrl = null);
public sealed record SiteRequest(string Name, string Tagline);
public sealed record PageRequest(string Slug, string Title, string Description, ContentBlock[] Blocks, Guid? Version);
public sealed record PublishRequest(Guid Version, Guid? RevisionId = null);
