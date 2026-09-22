using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity.Security;

internal sealed class PortalRecipientDirectory(NexoraIdentityDbContext db) : IPortalRecipientDirectory
{
    public Task<Guid?> FindPortalUserIdAsync(Guid tenantId, Guid contactId, CancellationToken ct) => db.PortalAccess
        .Where(x => x.TenantId == tenantId && x.ContactId == contactId && !x.IsRevoked)
        .Select(x => (Guid?)x.UserId).FirstOrDefaultAsync(ct);
}
