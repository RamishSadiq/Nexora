using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Work.Domain;
namespace Nexora.Modules.Work.Infrastructure;
public sealed class WorkDbContext(DbContextOptions<WorkDbContext> options,IRequestIdentity identity,IIdentityDirectory? directory=null):TenantDbContext(options,identity)
{
 protected override void OnModelCreating(ModelBuilder b){b.HasDefaultSchema("work");TenantTable<WorkItem>(b,"Items");TenantTable<Notification>(b,"Notifications").HasQueryFilter(x=>x.TenantId==ActiveTenantId&&x.UserId==ActiveUserId);TenantTable<WorkHistory>(b,"History");TenantTable<ImportBatch>(b,"ImportBatches");TenantTable<ImportRow>(b,"ImportRows");b.Entity<ImportRow>().HasOne<ImportBatch>().WithMany().HasForeignKey(x=>new{x.TenantId,x.BatchId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.Entity<Notification>().HasOne<WorkItem>().WithMany().HasForeignKey(x=>new{x.TenantId,x.WorkItemId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);ConfigureColumns(b);}
 protected override async Task ValidateEntityAsync(EntityEntry<TenantEntity> entry,CancellationToken ct)
 {
  if(entry.Entity is WorkItem row&&(entry.State==EntityState.Added||entry.Property(nameof(WorkItem.AssigneeUserId)).IsModified||entry.Property(nameof(WorkItem.TeamId)).IsModified)&&(row.AssigneeUserId!=null||row.TeamId!=null)&&(directory==null||!await directory.IsValidOwnerAsync(row.AssigneeUserId,row.TeamId,ct)))throw new TenantBoundaryException();
  if(entry.Entity is Notification note&&entry.State!=EntityState.Added){var stored=await entry.GetDatabaseValuesAsync(ct);if(note.UserId!=ActiveUserId||stored==null||!Equals(stored["UserId"],ActiveUserId))throw new TenantBoundaryException();}
 }
}
