using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Events.Domain;
namespace Nexora.Modules.Events.Infrastructure;
public sealed class CrmContextProvider(EventsDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["events.read"];
    public async Task<Guid[]> SearchRecordIdsAsync(string term, string kind, CancellationToken ct)
    {
        return await (from x in db.Set<Enrollment>() join e in db.Set<Offering>() on x.OfferingId equals e.Id where kind == "contact" && e.Name.Contains(term) select x.ContactId).Distinct().Take(1001).ToArrayAsync(ct);
    }
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var rows = db.Set<Enrollment>().Where(x => kind == "contact" && x.ContactId == id);
        return [new("Event registrations", await rows.CountAsync(ct), await (from x in rows join e in db.Set<Offering>() on x.OfferingId equals e.Id orderby x.CreatedAtUtc descending select new CrmContextItem(x.Id, e.Name, x.Status, x.CreatedAtUtc, "/events")).Take(50).ToListAsync(ct))];
    }
}
