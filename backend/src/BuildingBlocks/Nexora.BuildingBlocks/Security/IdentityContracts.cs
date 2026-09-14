namespace Nexora.BuildingBlocks.Security;

public interface IRequestIdentity
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
}

public sealed record DirectoryItem(Guid Id, string Name);
public sealed record IdentityDirectory(IReadOnlyList<DirectoryItem> Users, IReadOnlyList<DirectoryItem> Teams);
public interface IIdentityDirectory
{
    Task<IdentityDirectory> GetAsync(CancellationToken cancellationToken);
    Task<bool> IsValidOwnerAsync(Guid? userId, Guid? teamId, CancellationToken cancellationToken);
}
