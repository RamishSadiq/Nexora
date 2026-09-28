using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
namespace Nexora.Modules.Membership.Infrastructure;
public sealed class CrmContextProvider(MembershipDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["membership.read"];
    public async Task<Guid[]> SearchRecordIdsAsync(string term, string kind, CancellationToken ct)
    {
        var apps = from a in db.Set<MembershipApplication>() join p in db.Set<MembershipProduct>() on a.ProductId equals p.Id where p.Name.Contains(term) select kind == "account" ? a.AccountId : (Guid?)a.ContactId;
        var members = from a in db.Set<MemberSubscription>() join p in db.Set<MembershipProduct>() on a.ProductId equals p.Id where p.Name.Contains(term) select kind == "account" ? a.AccountId : (Guid?)a.ContactId;
        return await apps.Union(members).Where(x => x != null).Select(x => x!.Value).Distinct().Take(1001).ToArrayAsync(ct);
    }
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var apps = db.Set<MembershipApplication>().Where(x => kind == "account" ? x.AccountId == id : x.ContactId == id);
        var members = db.Set<MemberSubscription>().Where(x => kind == "account" ? x.AccountId == id : x.ContactId == id);
        var notifications = db.Set<PortalNotification>().Where(x => apps.Any(a => a.Id == x.ApplicationId));
        var history = db.Set<MembershipHistory>().Where(x => apps.Any(a => a.Id == x.SubjectId) || members.Any(m => m.Id == x.SubjectId));
        return [new("Applications", await apps.CountAsync(ct), await (from a in apps join p in db.Set<MembershipProduct>() on a.ProductId equals p.Id orderby a.CreatedAtUtc descending select new CrmContextItem(a.Id, p.Name, a.Status, a.CreatedAtUtc, "/membership")).Take(50).ToListAsync(ct)),
        new("Memberships", await members.CountAsync(ct), await (from m in members join p in db.Set<MembershipProduct>() on m.ProductId equals p.Id orderby m.CreatedAtUtc descending select new CrmContextItem(m.Id, p.Name, m.Status, m.CreatedAtUtc, "/membership")).Take(50).ToListAsync(ct)),
        new("Portal notifications", await notifications.CountAsync(ct), await notifications.OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new CrmContextItem(x.Id, x.Subject, x.DeliveryStatus, x.CreatedAtUtc, "/membership")).ToListAsync(ct)),
        new("Membership history", await history.CountAsync(ct), await history.OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new CrmContextItem(x.Id, x.Action, "recorded", x.CreatedAtUtc, "/membership")).ToListAsync(ct))];
    }
}
