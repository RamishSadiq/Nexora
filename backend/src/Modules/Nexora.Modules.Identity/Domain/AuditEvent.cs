namespace Nexora.Modules.Identity.Domain;

public sealed class AuditEvent
{
    public long Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string EventType { get; set; }
    public required string Outcome { get; set; }
    public string? Subject { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
