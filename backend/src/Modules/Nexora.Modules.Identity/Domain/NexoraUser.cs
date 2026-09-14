using Microsoft.AspNetCore.Identity;

namespace Nexora.Modules.Identity.Domain;

public sealed class NexoraUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
