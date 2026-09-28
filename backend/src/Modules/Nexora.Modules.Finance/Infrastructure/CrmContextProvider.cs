using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Finance.Domain;
namespace Nexora.Modules.Finance.Infrastructure;
public sealed class CrmContextProvider(FinanceDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["finance.read"];
    public async Task<Guid[]> SearchRecordIdsAsync(string term, string kind, CancellationToken ct)
    {
        return await db.Set<PostedInvoice>().Where(x => x.Number.Contains(term)).Select(x => x.CustomerId).Distinct().Take(1001).ToArrayAsync(ct);
    }
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var rows = db.Set<PostedInvoice>().Where(x => x.CustomerId == id);
        return [new("Invoices", await rows.CountAsync(ct), await rows.OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new CrmContextItem(x.Id, x.Number, "posted", x.CreatedAtUtc, "/finance")).ToListAsync(ct))];
    }
}
