using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
namespace Nexora.Modules.Identity.Security;
public sealed class CrmPortalContext(NexoraIdentityDbContext db) : ICrmContextProvider
{
    public string[] Permissions => ["identity.manage", "crm.manage"];
    public async Task<IReadOnlyList<CrmContextSection>> ReadAsync(Guid id, string kind, CancellationToken ct)
    {
        var access = db.PortalAccess.Where(x => kind == "contact" && x.ContactId == id);
        var company = from g in db.Set<PortalOrganisationGrant>() join a in db.PortalAccess on g.PortalAccessId equals a.Id
                      where kind == "account" ? g.AccountId == id : a.ContactId == id
                      select new { g.Id, g.IsRevoked, ParentRevoked = a.IsRevoked, g.CanSubmitApplications, g.CreatedAtUtc };
        return [new("Portal access", await access.CountAsync(ct), await access.OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new CrmContextItem(x.Id, "Personal access grant", x.IsRevoked ? "revoked" : "granted", x.CreatedAtUtc, "/settings")).ToListAsync(ct)),
                new("Company portal access", await company.CountAsync(ct), await company.OrderByDescending(x => x.CreatedAtUtc).Take(50).Select(x => new CrmContextItem(x.Id, "Company access grant", x.IsRevoked || x.ParentRevoked ? "revoked" : x.CanSubmitApplications ? "view and apply" : "view only", x.CreatedAtUtc, "/settings")).ToListAsync(ct))];
    }
}
