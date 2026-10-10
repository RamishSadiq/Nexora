using Nexora.BuildingBlocks.Domain;

namespace Nexora.Modules.Membership.Domain;

public sealed class MembershipClassificationChange : TenantEntity, IAppendOnly
{
    public Guid MembershipId { get; set; }
    public string Kind { get; set; } = "category";
    public Guid? DefinitionId { get; set; }
    public string? NameSnapshot { get; set; }
    public DateOnly EffectiveOn { get; set; }
    public int Sequence { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = "";
}
