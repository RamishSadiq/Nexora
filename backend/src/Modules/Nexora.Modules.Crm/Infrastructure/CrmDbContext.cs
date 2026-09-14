using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;

namespace Nexora.Modules.Crm.Infrastructure;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options, IRequestIdentity identity, IIdentityDirectory? directory = null) : DbContext(options)
{
    public DbSet<CrmRecord> Records => Set<CrmRecord>();
    private Guid? TenantId => identity.TenantId;
    private Guid? UserId => identity.UserId;
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("crm");
        Configure<CrmRecord>(b, "Records");
        Configure<Address>(b, "Addresses");
        Configure<CommunicationMethod>(b, "CommunicationMethods");
        Configure<RecordRelationship>(b, "Relationships");
        Configure<Note>(b, "Notes");
        Configure<RecordTag>(b, "Tags");
        Configure<RecordFile>(b, "Files");
        Configure<Activity>(b, "Activities");
        Configure<CustomFieldDefinition>(b, "CustomFieldDefinitions");
        Configure<CustomFieldValue>(b, "CustomFieldValues");
        Configure<SavedView>(b, "SavedViews");
        b.Entity<SavedView>().HasQueryFilter(x => x.TenantId == TenantId && x.UserId == UserId);
        b.Entity<CrmRecord>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<CrmRecord>().HasIndex(x => new { x.TenantId, x.Kind, x.IsArchived, x.Name, x.Id });
        b.Entity<CrmRecord>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<CrmRecord>().Property(x => x.Kind).HasMaxLength(20);
        b.Entity<CrmRecord>().Property(x => x.Status).HasMaxLength(20);
        Child<Address>(b); Child<CommunicationMethod>(b); Child<RecordRelationship>(b);
        Child<Note>(b); Child<RecordTag>(b); Child<RecordFile>(b); Child<Activity>(b); Child<CustomFieldValue>(b);
        b.Entity<RecordRelationship>().HasOne<CrmRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.TargetRecordId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CustomFieldValue>().HasOne<CustomFieldDefinition>().WithMany().HasForeignKey(x => new { x.TenantId, x.DefinitionId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CustomFieldValue>().Property(x => x.NumberValue).HasPrecision(18, 4);
        b.Entity<CustomFieldValue>().HasIndex(x => new { x.TenantId, x.RecordId, x.DefinitionId }).IsUnique();
        b.Entity<RecordTag>().HasIndex(x => new { x.TenantId, x.RecordId, x.Name }).IsUnique();
        b.Entity<RecordTag>().Property(x => x.Name).HasMaxLength(80);
        b.Entity<CustomFieldDefinition>().Property(x => x.Name).HasMaxLength(80);
        b.Entity<CustomFieldDefinition>().HasIndex(x => new { x.TenantId, x.Kind, x.Name }).IsUnique();
        b.Entity<RecordFile>().Property(x => x.Content).HasMaxLength(5 * 1024 * 1024);
        var utc = new ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(x => x.ClrType == typeof(DateTime) || x.ClrType == typeof(DateTime?)))
                property.SetValueConverter(utc);
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(x => x.ClrType == typeof(string) && x.GetMaxLength() == null))
                property.SetMaxLength(property.Name == "Body" ? 4000 : 500);
    }
    private void Configure<T>(ModelBuilder b, string table) where T : TenantRow
    {
        b.Entity<T>().HasBaseType((Type?)null);
        b.Entity<T>().ToTable(table).HasKey(x => x.Id);
        b.Entity<T>().HasAlternateKey(x => new { x.TenantId, x.Id });
        b.Entity<T>().HasQueryFilter(x => x.TenantId == TenantId);
    }
    private static void Child<T>(ModelBuilder b) where T : RecordChild => b.Entity<T>()
        .HasOne<CrmRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.RecordId })
        .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardAsync(default).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await GuardAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    private async Task GuardAsync(CancellationToken ct)
    {
        ChangeTracker.DetectChanges();
        foreach (var e in ChangeTracker.Entries<TenantRow>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            if (TenantId == null || UserId == null || e.Entity.TenantId != TenantId) throw new CrmBoundaryException();
            if (e.Entity is CrmRecord record && (e.State == EntityState.Added || e.Property(nameof(CrmRecord.OwnerUserId)).IsModified || e.Property(nameof(CrmRecord.OwnerTeamId)).IsModified) &&
                (record.OwnerUserId != null || record.OwnerTeamId != null) &&
                (directory == null || !await directory.IsValidOwnerAsync(record.OwnerUserId, record.OwnerTeamId, ct))) throw new CrmBoundaryException();
            if (e.Entity is SavedView view && view.UserId != UserId) throw new CrmBoundaryException();
            if (e.Entity is Activity && e.State != EntityState.Added) throw new CrmBoundaryException();
            if (e.State != EntityState.Added)
            {
                var stored = await e.GetDatabaseValuesAsync(ct);
                if (stored == null || !Equals(stored["TenantId"], TenantId) ||
                    (e.Entity is SavedView && !Equals(stored["UserId"], UserId))) throw new CrmBoundaryException();
            }
        }
    }
}
public sealed class CrmBoundaryException() : InvalidOperationException("Operation outside the active CRM boundary.");
