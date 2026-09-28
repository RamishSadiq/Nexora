using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Engagement.Domain;
namespace Nexora.Modules.Engagement.Infrastructure;
public sealed class CrmContextProvider(EngagementDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["engagement.read"];
    public async Task<Guid[]> SearchRecordIdsAsync(string term, string kind, CancellationToken ct)
    {
        return await (from x in db.Set<CommunityMember>() join e in db.Set<Community>() on x.CommunityId equals e.Id where kind == "contact" && e.Name.Contains(term) select x.ContactId).Distinct().Take(1001).ToArrayAsync(ct);
    }
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var rows = db.Set<CommunityMember>().Where(x => kind == "contact" && x.ContactId == id);
        return [new("Groups and committees", await rows.CountAsync(ct), await (from x in rows join c in db.Set<Community>() on x.CommunityId equals c.Id orderby x.CreatedAtUtc descending select new CrmContextItem(x.Id, c.Name, x.Role, x.CreatedAtUtc, "/engagement")).Take(50).ToListAsync(ct))];
    }
}
