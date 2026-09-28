using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
namespace Nexora.Modules.Membership.Infrastructure;
public sealed class CrmEvidenceContext(MembershipDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["membership.read", "crm.files"];
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var rows = from e in db.Set<ApplicationEvidence>() join d in db.Set<PortalApplicationDraft>() on e.DraftId equals d.Id
                   join a in db.Set<MembershipApplication>() on d.ApplicationId equals a.Id
                   where kind == "account" ? a.AccountId == id : a.ContactId == id
                   select new { e.Id, e.Name, e.ScanStatus, e.CreatedAtUtc, ApplicationId = a.Id };
        var items = await rows.OrderByDescending(x => x.CreatedAtUtc).Take(50).ToListAsync(ct);
        return [new("Submitted application documents", await rows.CountAsync(ct), items.Select(x => new CrmContextItem(x.Id, x.Name, x.ScanStatus, x.CreatedAtUtc, x.ScanStatus == "clean" ? $"/api/v1/membership/applications/{x.ApplicationId}/evidence/{x.Id}" : "/membership")).ToArray())];
    }
}
