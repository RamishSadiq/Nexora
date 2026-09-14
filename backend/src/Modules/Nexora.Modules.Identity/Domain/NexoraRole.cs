using Microsoft.AspNetCore.Identity;

namespace Nexora.Modules.Identity.Domain;

public sealed class NexoraRole : IdentityRole<Guid>
{
    public Guid TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}
