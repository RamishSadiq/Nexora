namespace Nexora.BuildingBlocks.Security;
public interface IScheduledPublisherDirectory
{
    Task<bool> CanPublishAsync(Guid tenantId, Guid userId, CancellationToken ct);
}
