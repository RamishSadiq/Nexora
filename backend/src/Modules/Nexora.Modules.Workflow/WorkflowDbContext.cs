using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Workflow;
public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options,IRequestIdentity identity):DbContext(options)
{
 private Guid? TenantId=>identity.TenantId;
 protected override void OnModelCreating(ModelBuilder b)
 {
  b.HasDefaultSchema("workflow");Table<WorkflowDefinition>(b,"Definitions");Table<WorkflowVersion>(b,"Versions");Table<WorkflowRun>(b,"Runs");Table<WorkflowStep>(b,"Steps");Table<WorkflowAudit>(b,"Audit");Table<WorkflowToken>(b,"Tokens");Table<WorkflowSchedule>(b,"Schedules");Table<WorkflowConnector>(b,"Connectors");Table<WorkflowEmailTemplate>(b,"EmailTemplates");Table<WorkflowDelivery>(b,"Deliveries");Table<WorkflowWebhook>(b,"Webhooks");
  b.Entity<WorkflowDefinition>().Property(x=>x.DraftJson).HasMaxLength(262144);b.Entity<WorkflowVersion>().Property(x=>x.GraphJson).HasMaxLength(262144);
  b.Entity<WorkflowVersion>().HasIndex(x=>new{x.TenantId,x.DefinitionId,x.Number}).IsUnique();
  b.Entity<WorkflowRun>().HasIndex(x=>new{x.TenantId,x.WorkflowVersionId,x.RequestKey}).IsUnique();
  b.Entity<WorkflowRun>().HasIndex(x=>new{x.Status,x.DueAtUtc,x.LeaseUntilUtc});
  b.Entity<WorkflowStep>().HasIndex(x=>new{x.TenantId,x.RunId,x.Sequence}).IsUnique();
  b.Entity<WorkflowVersion>().HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x=>new{x.TenantId,x.DefinitionId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowRun>().HasOne<WorkflowVersion>().WithMany().HasForeignKey(x=>new{x.TenantId,x.WorkflowVersionId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowStep>().HasOne<WorkflowRun>().WithMany().HasForeignKey(x=>new{x.TenantId,x.RunId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowAudit>().HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x=>new{x.TenantId,x.DefinitionId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowStep>().Property(x=>x.InputsJson).HasMaxLength(262144);b.Entity<WorkflowStep>().Property(x=>x.OutputsJson).HasMaxLength(262144);b.Entity<WorkflowStep>().Property(x=>x.Scope).HasMaxLength(4000);
  b.Entity<WorkflowRun>().Property(x=>x.ContextJson).HasMaxLength(262144);b.Entity<WorkflowRun>().Property(x=>x.TriggerJson).HasMaxLength(262144);b.Entity<WorkflowRun>().Property(x=>x.AncestryJson).HasMaxLength(4000);
  b.Entity<WorkflowToken>().Property(x=>x.ContextJson).HasMaxLength(262144);b.Entity<WorkflowToken>().Property(x=>x.FramesJson).HasMaxLength(262144);b.Entity<WorkflowToken>().Property(x=>x.PendingInputsJson).HasMaxLength(262144);b.Entity<WorkflowToken>().Property(x=>x.Scope).HasMaxLength(4000);
  b.Entity<WorkflowToken>().HasIndex(x=>new{x.Status,x.DueAtUtc,x.LeaseUntilUtc});
  b.Entity<WorkflowToken>().HasOne<WorkflowRun>().WithMany().HasForeignKey(x=>new{x.TenantId,x.RunId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowSchedule>().HasIndex(x=>new{x.TenantId,x.DefinitionId}).IsUnique();b.Entity<WorkflowSchedule>().HasIndex(x=>new{x.IsEnabled,x.NextAtUtc});
  b.Entity<WorkflowSchedule>().HasOne<WorkflowVersion>().WithMany().HasForeignKey(x=>new{x.TenantId,x.WorkflowVersionId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowConnector>().Property(x=>x.Endpoint).HasMaxLength(2000);b.Entity<WorkflowConnector>().Property(x=>x.ProtectedSecret).HasMaxLength(16000);
  Table<WorkflowApproval>(b,"Approvals");
  b.Entity<WorkflowApproval>().HasIndex(x=>new{x.TenantId,x.RunId,x.TokenId,x.NodeId}).IsUnique();
  b.Entity<WorkflowApproval>().HasIndex(x=>new{x.TenantId,x.Status,x.DeadlineUtc});
  b.Entity<WorkflowApproval>().HasOne<WorkflowRun>().WithMany().HasForeignKey(x=>new{x.TenantId,x.RunId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowApproval>().HasOne<WorkflowToken>().WithMany().HasForeignKey(x=>new{x.TenantId,x.TokenId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowEmailTemplate>().Property(x=>x.Body).HasMaxLength(16000);
  b.Entity<WorkflowDelivery>().Property(x=>x.RequestJson).HasMaxLength(262144);b.Entity<WorkflowDelivery>().Property(x=>x.ResultJson).HasMaxLength(262144);
  b.Entity<WorkflowDelivery>().HasOne<WorkflowConnector>().WithMany().HasForeignKey(x=>new{x.TenantId,x.ConnectorId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<WorkflowWebhook>().Property(x=>x.ProtectedSecret).HasMaxLength(16000);b.Entity<WorkflowWebhook>().HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x=>new{x.TenantId,x.DefinitionId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  var utc=new ValueConverter<DateTime,DateTime>(value=>value,value=>DateTime.SpecifyKind(value,DateTimeKind.Utc));
  foreach(var entity in b.Model.GetEntityTypes())foreach(var property in entity.GetProperties().Where(p=>p.ClrType==typeof(DateTime)||p.ClrType==typeof(DateTime?)))property.SetValueConverter(utc);
 }
 private void Table<T>(ModelBuilder b,string name) where T:TenantEntity
 {
  var e=b.Entity<T>();e.HasBaseType((Type?)null);e.ToTable(name);e.HasKey(x=>x.Id);e.HasAlternateKey(x=>new{x.TenantId,x.Id});e.HasQueryFilter(x=>x.TenantId==TenantId);e.Property(x=>x.Version).IsConcurrencyToken();
  foreach(var p in e.Metadata.GetProperties().Where(p=>p.ClrType==typeof(string)))p.SetMaxLength(500);
 }
 public override int SaveChanges(bool acceptAllChangesOnSuccess)=>SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
 public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken ct=default)
 {
  foreach(var e in ChangeTracker.Entries<TenantEntity>().Where(x=>x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
  {
   if(TenantId==null||identity.UserId==null||e.Entity.TenantId!=TenantId)throw new TenantBoundaryException();
   if(e.Entity is IAppendOnly&&e.State!=EntityState.Added)throw new TenantBoundaryException();
   if(e.State!=EntityState.Added){var stored=await e.GetDatabaseValuesAsync(ct);if(stored==null||!Equals(stored["TenantId"],TenantId))throw new TenantBoundaryException();e.Entity.CreatedAtUtc=(DateTime)stored["CreatedAtUtc"]!;e.Entity.UpdatedAtUtc=DateTime.UtcNow;e.Entity.Version=Guid.NewGuid();}
  }
  return await base.SaveChangesAsync(acceptAllChangesOnSuccess,ct);
 }
}
