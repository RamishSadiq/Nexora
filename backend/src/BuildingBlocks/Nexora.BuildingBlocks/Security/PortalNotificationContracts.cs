namespace Nexora.BuildingBlocks.Security;

public sealed record PortalNotificationItem(Guid Id, string Kind, string Subject, string Body, string DeliveryStatus, bool IsRead, DateTime CreatedAtUtc, Guid? ApplicationId);
public interface IPortalNotificationService
{
    Task<IReadOnlyList<PortalNotificationItem>> ListAsync(Guid userId, CancellationToken ct);
    Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct);
    Task EnqueueAsync(Guid tenantId, Guid userId, Guid? applicationId, string kind, string subject, string body, CancellationToken ct);
}
public interface IPortalRecipientDirectory
{
    Task<Guid?> FindPortalUserIdAsync(Guid tenantId, Guid contactId, CancellationToken ct);
}
