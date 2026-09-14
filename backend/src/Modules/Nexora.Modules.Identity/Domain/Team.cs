namespace Nexora.Modules.Identity.Domain;

public sealed class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TeamMember> Members { get; set; } = [];
}

public sealed class TeamMember
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid UserId { get; set; }
    public NexoraUser User { get; set; } = null!;
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
