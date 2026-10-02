using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
namespace Nexora.Modules.Identity.Security;
internal sealed class WorkflowPrincipalDirectory(NexoraIdentityDbContext db):IWorkflowPrincipalDirectory
{
 public async Task<WorkflowPrincipal?> ResolveAsync(Guid tenantId,Guid userId,CancellationToken ct)
 {
  if(!await db.Tenants.IgnoreQueryFilters().AnyAsync(x=>x.Id==tenantId&&x.Status==TenantStatus.Active,ct))return null;
  var user=await db.Users.IgnoreQueryFilters().Where(x=>x.Id==userId&&x.TenantId==tenantId&&x.IsActive).Select(x=>new{x.LockoutEnd}).SingleOrDefaultAsync(ct);
  if(user==null||user.LockoutEnd>DateTimeOffset.UtcNow)return null;
  var roles=await (from ur in db.UserRoles.IgnoreQueryFilters() join r in db.Roles.IgnoreQueryFilters() on ur.RoleId equals r.Id where ur.UserId==userId&&r.TenantId==tenantId select new{r.Id,r.IsSystem}).ToListAsync(ct);
  var ids=roles.Select(x=>x.Id).ToArray();
  var permissions=roles.Any(x=>x.IsSystem)?NexoraPermissions.All:await db.RolePermissions.IgnoreQueryFilters().Where(x=>ids.Contains(x.RoleId)).Select(x=>x.Permission).Distinct().ToArrayAsync(ct);
  return new(tenantId,userId,permissions);
 }
}
