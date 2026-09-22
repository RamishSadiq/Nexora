namespace Nexora.BuildingBlocks.Security;
public sealed record CrmIdentity(Guid Id, string Name, string Kind);
public interface ICrmDirectory
{
    Task<string?> MarketingEmailAsync(Guid contactId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CrmIdentity>> SearchContactsAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<CrmIdentity>> SearchAccountsAsync(string? search, CancellationToken cancellationToken);
    Task<bool> HasCurrentAccountContactAsync(Guid accountId, Guid contactId, CancellationToken cancellationToken);
    Task<CrmIdentity?> FindActiveAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record MemberProfile(string Name, Guid Version);
public interface IMemberProfileDirectory
{
    Task<MemberProfile?> ReadAsync(Guid contactId, CancellationToken ct);
    Task<MemberProfile?> UpdateNameAsync(Guid contactId, string name, Guid version, CancellationToken ct);
}
