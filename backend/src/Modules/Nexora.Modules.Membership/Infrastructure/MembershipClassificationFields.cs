using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;

namespace Nexora.Modules.Membership.Infrastructure;

public static class MembershipClassificationFields
{
    public static async Task<MembershipClassificationChange[]> CurrentChangesAsync(MembershipDbContext db, Guid[] ids, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Set<MembershipClassificationChange>().AsNoTracking().Where(x => ids.Contains(x.MembershipId) && x.EffectiveOn <= today)
            .GroupBy(x => new { x.MembershipId, x.Kind }).Select(group => group.OrderByDescending(x => x.EffectiveOn).ThenByDescending(x => x.Sequence).First()).ToArrayAsync(ct);
    }
    public static MembershipClassifications Read(Guid id, IEnumerable<MembershipClassificationChange> changes)
    {
        MembershipClassification For(string kind)
        {
            var change = changes.SingleOrDefault(x => x.MembershipId == id && x.Kind == kind);
            return new(change?.DefinitionId, change?.NameSnapshot, change?.EffectiveOn);
        }
        return new(For("category"), For("subscription-band"), For("member-type"));
    }
}
