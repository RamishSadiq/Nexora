using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.BuildingBlocks.Persistence;

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
        Configure<ContactProfile>(b, "ContactProfiles");
        Configure<AccountProfile>(b, "AccountProfiles");
        Child<ContactProfile>(b); Child<AccountProfile>(b);
        b.Entity<ContactProfile>().HasIndex(x => new { x.TenantId, x.RecordId }).IsUnique();
        b.Entity<AccountProfile>().HasIndex(x => new { x.TenantId, x.RecordId }).IsUnique();
        Configure<CommunicationMethod>(b, "CommunicationMethods");
        Configure<RecordRelationship>(b, "Relationships");
        Configure<AccountContact>(b, "AccountContacts");
        Configure<AccountContactRole>(b, "AccountContactRoles");
        b.Entity<AccountContact>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<AccountContact>().Property(x => x.Department).HasMaxLength(160);
        b.Entity<AccountContact>().Property(x => x.JobTitle).HasMaxLength(160);
        b.Entity<AccountContact>().Property(x => x.WorkClassification).HasMaxLength(30);
        b.Entity<AccountContact>().HasOne<CrmRecord>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AccountContact>().HasOne<CrmRecord>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.ContactId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AccountContact>().HasIndex(x => new { x.TenantId, x.AccountId, x.ContactId }).IsUnique().HasFilter("EndDate IS NULL");
        b.Entity<AccountContact>().HasIndex(x => new { x.TenantId, x.ContactId, x.EndDate });
        b.Entity<AccountContact>().HasIndex(x => new { x.TenantId, x.ContactId }).IsUnique()
            .HasFilter("[IsMainAccount] = 1 AND [EndDate] IS NULL");
        b.Entity<AccountContactRole>().Property(x => x.RoleCode).HasMaxLength(50);
        b.Entity<AccountContactRole>().HasOne<AccountContact>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.AssociationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AccountContactRole>().HasIndex(x => new { x.TenantId, x.AssociationId, x.RoleCode }).IsUnique();
        Configure<Note>(b, "Notes");
        Configure<RecordTag>(b, "Tags");
        Configure<RecordFile>(b, "Files");
        b.Entity<RecordFile>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<RecordFile>().Property(x => x.Sha256).HasMaxLength(64);
        b.Entity<RecordFile>().Property(x => x.ScanStatus).HasMaxLength(20);
        Configure<Activity>(b, "Activities");
        Configure<CustomFieldDefinition>(b, "CustomFieldDefinitions");
        Configure<CustomFieldValue>(b, "CustomFieldValues");
        Configure<SavedView>(b, "SavedViews");
        b.Entity<SavedView>().Property(x => x.Columns).HasMaxLength(4000);
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
        Automations.Configure(b);
        b.Entity<AutomationConfiguration>().HasQueryFilter(x => x.TenantId == TenantId);
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
        AutomateAsync(default).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await AutomateAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
    public async Task<AutomationField[]> AutomationFieldsAsync(string kind, CancellationToken ct)
    {
        await Set<CustomFieldDefinition>().Where(x => x.Kind == kind).LoadAsync(ct);
        return ChangeTracker.Entries<CustomFieldDefinition>().Where(e => e.State != EntityState.Deleted && e.Entity.TenantId == TenantId && e.Entity.Kind == kind)
            .Select(e => new AutomationField(e.Entity.Id.ToString(), e.Entity.Name, e.Entity.DataType, true)).ToArray();
    }
    private async Task AutomateAsync(CancellationToken ct)
    {
        await GuardAsync(ct);
        await Automations.GuardAsync(this, TenantId, UserId, ct);
        await Automations.ApplyAsync<CustomFieldValue>(this, TenantId, kind => kind is "contact" or "account" ? typeof(CrmRecord) : null,
            x => ((CrmRecord)x).Kind, AutomationFieldsAsync, ct);
        await GuardAsync(ct);
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
            if (e.Entity is AccountContact && e.State == EntityState.Deleted) throw new CrmBoundaryException();
            if (e.Entity is ContactProfile or AccountProfile)
            {
                var profile = (RecordChild)e.Entity;
                var kind = profile is ContactProfile ? "contact" : "account";
                if (!await Records.AnyAsync(x => x.Id == profile.RecordId && x.Kind == kind, ct)) throw new CrmBoundaryException();
                if (e.State != EntityState.Added && e.Property(nameof(RecordChild.RecordId)).IsModified) throw new CrmBoundaryException();
            }
            if (e.State != EntityState.Added)
            {
                var stored = await e.GetDatabaseValuesAsync(ct);
                if (stored == null || !Equals(stored["TenantId"], TenantId) ||
                    (e.Entity is SavedView && !Equals(stored["UserId"], UserId))) throw new CrmBoundaryException();
                if (e.Entity is AccountContact &&
                    (!Equals(stored[nameof(AccountContact.AccountId)], e.CurrentValues[nameof(AccountContact.AccountId)]) ||
                     !Equals(stored[nameof(AccountContact.ContactId)], e.CurrentValues[nameof(AccountContact.ContactId)]) ||
                     !Equals(stored[nameof(AccountContact.StartDate)], e.CurrentValues[nameof(AccountContact.StartDate)]) ||
                     stored[nameof(AccountContact.EndDate)] != null))
                    throw new CrmBoundaryException();
            }
            if (e.Entity is AccountContact link && e.State != EntityState.Deleted)
            {
                if (!await Records.AnyAsync(x => x.Id == link.AccountId && x.Kind == "account", ct) ||
                    !await Records.AnyAsync(x => x.Id == link.ContactId && x.Kind == "contact", ct)) throw new CrmBoundaryException();
                if (link.Department?.Length > 160 || link.Department?.Any(char.IsControl) == true)
                    throw new System.ComponentModel.DataAnnotations.ValidationException("Department must be at most 160 characters without control characters.");
                if (!AccountContactChoices.WorkClassifications.Contains(link.WorkClassification) ||
                    link.StartDate == default || link.StartDate.TimeOfDay != TimeSpan.Zero || link.StartDate > DateTime.UtcNow.Date ||
                    (link.EndDate.HasValue && (link.EndDate < link.StartDate || link.EndDate > DateTime.UtcNow.Date || link.EndDate.Value.TimeOfDay != TimeSpan.Zero)))
                    throw new System.ComponentModel.DataAnnotations.ValidationException("Invalid association classification or dates.");
            }
            if (e.Entity is AccountContactRole role && e.State != EntityState.Deleted && !AccountContactChoices.Roles.Contains(role.RoleCode))
                throw new System.ComponentModel.DataAnnotations.ValidationException("Unsupported contact role.");
        }
    }
}
public sealed class CrmBoundaryException() : InvalidOperationException("Operation outside the active CRM boundary.");
