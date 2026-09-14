namespace Nexora.BuildingBlocks.Security;
public sealed record CrmIdentity(Guid Id, string Name, string Kind);
public interface ICrmDirectory
{
    Task<string?> MarketingEmailAsync(Guid contactId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CrmIdentity>> SearchContactsAsync(string? search, CancellationToken cancellationToken);
    Task<CrmIdentity?> FindActiveAsync(Guid id, CancellationToken cancellationToken);
}
