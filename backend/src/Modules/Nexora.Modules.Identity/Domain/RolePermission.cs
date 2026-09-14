namespace Nexora.Modules.Identity.Domain;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public NexoraRole Role { get; set; } = null!;
    public required string Permission { get; set; }
}
