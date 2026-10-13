namespace Nexora.Modules.Identity.Domain;

public sealed class PortalOrganisationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PortalAccessId { get; set; }
    public string OrganisationName { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "pending";
    public Guid Version { get; set; } = Guid.NewGuid();
    public Guid? AccountId { get; set; }
    public bool CanSubmitApplications { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedBy { get; set; }
    public string DecisionMessage { get; set; } = "";
}
