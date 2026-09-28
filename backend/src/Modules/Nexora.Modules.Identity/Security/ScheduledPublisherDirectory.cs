using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Domain;

namespace Nexora.Modules.Identity.Security;
internal sealed class ScheduledPublisherDirectory(NexoraIdentityDbContext db) : IScheduledPublisherDirectory
{
    public async Task<bool> CanPublishAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == tenantId && t.Status == TenantStatus.Active, ct)) return false;
        var user = await db.Users.IgnoreQueryFilters().Where(u => u.Id == userId && u.TenantId == tenantId && u.IsActive)
            .Select(u => new { u.LockoutEnd }).SingleOrDefaultAsync(ct);
        if (user == null || user.LockoutEnd > DateTimeOffset.UtcNow) return false;
        var permissions = await (from ur in db.UserRoles.IgnoreQueryFilters()
            join role in db.Roles.IgnoreQueryFilters() on ur.RoleId equals role.Id
            join permission in db.RolePermissions.IgnoreQueryFilters() on role.Id equals permission.RoleId
            where ur.UserId == userId && role.TenantId == tenantId
            select permission.Permission).ToListAsync(ct);
        return permissions.Contains("content.read") && permissions.Contains("content.publish");
    }
}
