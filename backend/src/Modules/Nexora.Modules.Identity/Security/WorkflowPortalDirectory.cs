using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
namespace Nexora.Modules.Identity.Security;
internal sealed class WorkflowPortalDirectory(NexoraIdentityDbContext db,ICrmDirectory crm,IRequestIdentity who):IWorkflowPortalDirectory
{
 public async Task<WorkflowPortalReview> ReviewAsync(Guid contactId,CancellationToken ct)
 {
  var access=await db.PortalAccess.Where(x=>x.TenantId==who.TenantId&&x.ContactId==contactId&&!x.IsRevoked).ToArrayAsync(ct);var issues=new List<string>();var invalid=0;
  foreach(var row in access){if(!await db.Users.AnyAsync(x=>x.TenantId==who.TenantId&&x.Id==row.UserId&&x.IsActive,ct))issues.Add("Bound portal user is inactive");foreach(var grant in await db.Set<PortalOrganisationGrant>().Where(x=>x.TenantId==who.TenantId&&x.PortalAccessId==row.Id&&!x.IsRevoked).ToArrayAsync(ct)){if((await crm.FindActiveAsync(grant.AccountId,ct))?.Kind!="account"||!await crm.HasCurrentAccountContactAsync(grant.AccountId,contactId,ct)){invalid++;issues.Add("Delegation no longer has an active organisation relationship");}}}
  return new(access.Length>0,invalid,string.Join("; ",issues.Distinct()));
 }
}
