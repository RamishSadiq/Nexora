namespace Nexora.Modules.Identity.Domain;

// Explicit access grant. CRM relationships and email addresses never imply this grant.
public sealed class PortalAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ContactId { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
