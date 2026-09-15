using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Security;
namespace Nexora.BuildingBlocks.Persistence;
public abstract class TenantDbContext(DbContextOptions options, IRequestIdentity identity) : DbContext(options)
{
    protected Guid? ActiveTenantId => identity.TenantId;
    protected Guid? ActiveUserId => identity.UserId;
    protected EntityTypeBuilder<T> TenantTable<T>(ModelBuilder builder, string table) where T : TenantEntity
    {
        var entity = builder.Entity<T>();
        entity.HasBaseType((Type?)null); entity.ToTable(table); entity.HasKey(x => x.Id);
        entity.HasAlternateKey(x => new { x.TenantId, x.Id });
        entity.HasQueryFilter(x => x.TenantId == ActiveTenantId);
        entity.Property(x => x.Version).IsConcurrencyToken();
        return entity;
    }
    protected void ConfigureColumns(ModelBuilder builder)
    {
        TenantTable<AttributeDefinition>(builder, "AttributeDefinitions");
        TenantTable<AttributeValue>(builder, "AttributeValues");
        builder.Entity<AttributeDefinition>().Property(x => x.Name).HasMaxLength(80);
        builder.Entity<AttributeDefinition>().Property(x => x.EntityType).HasMaxLength(80);
        builder.Entity<AttributeDefinition>().HasIndex(x => new { x.TenantId, x.EntityType, x.Name }).IsUnique();
        builder.Entity<AttributeValue>().HasIndex(x => new { x.TenantId, x.RecordId, x.DefinitionId }).IsUnique();
        builder.Entity<AttributeValue>().HasOne<AttributeDefinition>().WithMany().HasForeignKey(x => new { x.TenantId, x.DefinitionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        foreach (var entity in builder.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string) && property.GetMaxLength() == null) property.SetMaxLength(500);
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)) { property.SetPrecision(18); property.SetScale(2); }
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?)) property.SetValueConverter(utc);
            }
        builder.Entity<AttributeValue>().Property(x => x.NumberValue).HasPrecision(18, 4);
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { GuardAsync(default).GetAwaiter().GetResult(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    { await GuardAsync(cancellationToken); return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
    protected virtual Task ValidateEntityAsync(EntityEntry<TenantEntity> entry, CancellationToken ct) => Task.CompletedTask;
    private async Task GuardAsync(CancellationToken ct)
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<TenantEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            await ValidateEntityAsync(entry,ct);
            if (ActiveTenantId == null || ActiveUserId == null || entry.Entity.TenantId != ActiveTenantId) throw new TenantBoundaryException();
            if (entry.Entity is IAppendOnly && entry.State != EntityState.Added) throw new TenantBoundaryException();
            if (entry.State != EntityState.Added)
            {
                var stored = await entry.GetDatabaseValuesAsync(ct);
                if (stored == null || !Equals(stored["TenantId"], ActiveTenantId)) throw new TenantBoundaryException();
                entry.Property(x => x.CreatedAtUtc).CurrentValue = (DateTime)stored["CreatedAtUtc"]!;
                entry.Entity.Version = Guid.NewGuid(); entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
public sealed class TenantBoundaryException() : InvalidOperationException("Operation outside the active tenant boundary.");
