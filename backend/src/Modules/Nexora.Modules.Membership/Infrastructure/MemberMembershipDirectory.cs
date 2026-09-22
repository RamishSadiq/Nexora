using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
namespace Nexora.Modules.Membership.Infrastructure;

internal sealed class MemberMembershipDirectory(MembershipDbContext db) : IMemberMembershipDirectory
{
    public async Task<OwnMembershipOverview> ReadOrganisationAsync(Guid accountId, CancellationToken ct)
    {
        var memberships = await (from m in db.Set<MemberSubscription>().AsNoTracking()
            join p in db.Set<MembershipProduct>() on m.ProductId equals p.Id
            where m.AccountId == accountId
            orderby m.StartsAtUtc descending, m.Id
            select new OwnMembership(m.Id, p.Name, m.Status, m.StartsAtUtc, m.EndsAtUtc)).ToListAsync(ct);
        var applications = await (from a in db.Set<MembershipApplication>().AsNoTracking()
            join p in db.Set<MembershipProduct>() on a.ProductId equals p.Id
            where a.AccountId == accountId
            orderby a.CreatedAtUtc descending, a.Id
            select new OwnApplication(a.Id, p.Name, a.Status, a.CreatedAtUtc)).ToListAsync(ct);
        return new(memberships, applications);
    }
    public async Task<OwnMembershipOverview> ReadAsync(Guid contactId, CancellationToken ct)
    {
        // A Contact association is not an organisation access grant. Only personal records are exposed.
        var memberships = await (from m in db.Set<MemberSubscription>().AsNoTracking()
            join p in db.Set<MembershipProduct>() on m.ProductId equals p.Id
            where m.ContactId == contactId && m.AccountId == null
            orderby m.StartsAtUtc descending, m.Id
            select new OwnMembership(m.Id, p.Name, m.Status, m.StartsAtUtc, m.EndsAtUtc)).ToListAsync(ct);
        var applications = await (from a in db.Set<MembershipApplication>().AsNoTracking()
            join p in db.Set<MembershipProduct>() on a.ProductId equals p.Id
            where a.ContactId == contactId && a.AccountId == null && a.ApplicationType == "individual"
            orderby a.CreatedAtUtc descending, a.Id
            select new OwnApplication(a.Id, p.Name, a.Status, a.CreatedAtUtc)).ToListAsync(ct);
        return new(memberships, applications);
    }
}
