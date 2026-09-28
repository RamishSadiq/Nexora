using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Work.Domain;
namespace Nexora.Modules.Work.Infrastructure;
public sealed class CrmContextProvider(WorkDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["work.read"];
    public async Task<Guid[]> SearchRecordIdsAsync(string term, string kind, CancellationToken ct)
    {
        return await db.Set<WorkItem>().Where(x => kind == "contact" && x.ContactId != null && x.Title.Contains(term)).Select(x => x.ContactId!.Value).Distinct().Take(1001).ToArrayAsync(ct);
    }
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var rows = db.Set<WorkItem>().Where(x => kind == "contact" && x.ContactId == id);
        return [new("Tasks", await rows.CountAsync(ct), await rows.OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new CrmContextItem(x.Id, x.Title, x.Status, x.CreatedAtUtc, "/work")).ToListAsync(ct))];
    }
}
