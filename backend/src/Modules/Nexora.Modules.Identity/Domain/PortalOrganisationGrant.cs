namespace Nexora.Modules.Identity.Domain;

// Bound to one access grant, so revoking/replacing a login binding cannot revive delegation.
public sealed class PortalOrganisationGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PortalAccessId { get; set; }
    public Guid AccountId { get; set; }
    public bool CanSubmitApplications { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
