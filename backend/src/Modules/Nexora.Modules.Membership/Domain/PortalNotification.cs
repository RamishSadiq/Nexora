using Nexora.BuildingBlocks.Domain;

namespace Nexora.Modules.Membership.Domain;

public sealed class PortalNotification : TenantEntity
{
    public Guid RecipientUserId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string Kind { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string DeliveryStatus { get; set; } = "queued";
    public int DeliveryAttempts { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
