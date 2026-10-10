using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.BuildingBlocks.Persistence;

namespace Nexora.Modules.Crm.Infrastructure;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options, IRequestIdentity identity, IIdentityDirectory? directory = null) : DbContext(options), IWorkflowWriteIdentity, IAutomationRecordSources, IAutomationFieldProvider
{
    public DbSet<CrmRecord> Records => Set<CrmRecord>();
    public IRequestIdentity WorkflowIdentity=>identity;
    private Guid? TenantId => identity.TenantId;
    private Guid? UserId => identity.UserId;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { base.OnConfiguring(optionsBuilder); optionsBuilder.AddInterceptors(new DataAuditInterceptor(()=>TenantId)); }
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("crm");
        Configure<CrmRecord>(b, "Records");
        Configure<Address>(b, "Addresses");
        b.Entity<Address>().Property(x => x.Purpose).HasMaxLength(20).HasDefaultValue("other");
        b.Entity<Address>().HasIndex(x => new { x.TenantId, x.RecordId });
        b.Entity<Address>().HasIndex(x => new { x.TenantId, x.RecordId, x.Purpose })
            .IsUnique().HasFilter("[Purpose] IN ('mailing', 'billing')");
        Configure<ContactProfile>(b, "ContactProfiles");
        Configure<ContactSocialProfile>(b, "ContactSocialProfiles");
        Child<ContactSocialProfile>(b);
        b.Entity<ContactSocialProfile>().Property(x => x.Channel).HasMaxLength(30);
        b.Entity<ContactSocialProfile>().Property(x => x.Identifier).HasMaxLength(320);
        b.Entity<ContactSocialProfile>().HasIndex(x => new { x.TenantId, x.RecordId, x.Channel }).IsUnique();
        b.Entity<ContactProfile>().Property(x => x.Remarks).HasMaxLength(4000);
        Configure<AccountProfile>(b, "AccountProfiles");
        b.Entity<AccountProfile>().Property(x => x.SourceAccountId).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.TradingStatus).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.AccountType).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.IndustrySector).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.Venue).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.MarketingSource).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.TurnoverRange).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.EmployeesRange).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.Region).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.Branch).HasMaxLength(160);
        b.Entity<AccountProfile>().Property(x => x.OldReference).HasMaxLength(160);
        Child<ContactProfile>(b); Child<AccountProfile>(b);
        b.Entity<ContactProfile>().HasIndex(x => new { x.TenantId, x.RecordId }).IsUnique();
        b.Entity<AccountProfile>().HasIndex(x => new { x.TenantId, x.RecordId }).IsUnique();
        Configure<CommunicationMethod>(b, "CommunicationMethods");
        b.Entity<CommunicationMethod>().Property(x => x.Priority).HasMaxLength(20).HasDefaultValue("other");
        b.Entity<CommunicationMethod>().HasIndex(x => new { x.TenantId, x.RecordId });
        b.Entity<CommunicationMethod>().HasIndex(x => new { x.TenantId, x.RecordId, x.Kind, x.Priority })
            .IsUnique().HasFilter("[Priority] IN ('primary', 'secondary')");
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
        Configure<ProfileDefinition>(b, "ProfileDefinitions");
        b.Entity<ProfileDefinition>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<ProfileDefinition>().Property(x => x.ParentNode).HasMaxLength(160);
        b.Entity<ProfileDefinition>().Property(x => x.ChildNode).HasMaxLength(160);
        b.Entity<ProfileDefinition>().Property(x => x.NormalizedParent).HasMaxLength(160);
        b.Entity<ProfileDefinition>().Property(x => x.NormalizedChild).HasMaxLength(160);
        b.Entity<ProfileDefinition>().HasIndex(x => new { x.TenantId, x.NormalizedParent, x.NormalizedChild }).IsUnique();
        Configure<RecordProfileAssignment>(b, "ProfileAssignments");
        Child<RecordProfileAssignment>(b);
        b.Entity<RecordProfileAssignment>().HasOne<ProfileDefinition>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.DefinitionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RecordProfileAssignment>().HasIndex(x => new { x.TenantId, x.RecordId, x.Slot }).IsUnique();
        b.Entity<RecordProfileAssignment>().ToTable("ProfileAssignments", t => t.HasCheckConstraint("CK_ProfileAssignments_Slot", "[Slot] >= 1 AND [Slot] <= 5"));
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
        WorkflowOutboxModel.Configure(b);
        b.Entity<WorkflowDomainEvent>().HasQueryFilter(x=>x.TenantId==TenantId);
        b.Entity<WorkflowActionReceipt>().HasQueryFilter(x=>x.TenantId==TenantId);
        Automations.Configure(b);
        b.Entity<AutomationConfiguration>().HasQueryFilter(x => x.TenantId == TenantId);
        DataAuditModel.Configure(b,()=>TenantId);
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
        var custom = ChangeTracker.Entries<CustomFieldDefinition>().Where(e => e.State != EntityState.Deleted && e.Entity.TenantId == TenantId && e.Entity.Kind == kind)
            .Select(e => new AutomationField(e.Entity.Id.ToString(), e.Entity.Name, e.Entity.DataType, true)).ToArray();
        var profileType = kind == "contact" ? typeof(ContactProfile) : typeof(AccountProfile);
        var profileFields = EntityFieldCatalog.Properties(profileType).Where(p => p.Name is not ("Id" or "RecordId"))
            .Select(p => new AutomationField("profile:" + p.Name, "Profile · " + EntityFieldCatalog.Label(p.Name), EntityFieldCatalog.DataType(p.PropertyType) is "reference" ? "text" : EntityFieldCatalog.DataType(p.PropertyType)!, false));
        var socialFields = kind == "contact" ? EntityFieldCatalog.Properties(typeof(Nexora.Modules.Crm.Contracts.ContactSocialMediaDetails))
            .Where(p => p.PropertyType == typeof(string) && p.Name != "MetricsSource")
            .Select(p => new AutomationField("social:" + p.Name, "Social media · " + EntityFieldCatalog.Label(p.Name), "text", false)) : [];
        return custom.Concat(profileFields).Concat(socialFields).Concat(Enumerable.Range(1, 5).Select(slot => new AutomationField("assignment:" + slot, "Profile " + slot, "text", false))).ToArray();
    }
    public IEnumerable<Guid> ChangedRecordIds(string kind, object[] changed)
    {
        var profileRecords = changed.OfType<RecordChild>().Where(x => x is RecordProfileAssignment || (kind == "contact" ? x is ContactProfile or ContactSocialProfile : x is AccountProfile)).Select(x => x.RecordId);
        var definitions = changed.OfType<ProfileDefinition>().Select(x => x.Id).ToArray();
        return profileRecords.Concat(definitions.Length == 0 ? [] : Set<RecordProfileAssignment>().Where(x => definitions.Contains(x.DefinitionId)).Select(x => x.RecordId).ToArray());
    }

    public async Task<object?> ReadSourceAsync(object record, string source, CancellationToken ct)
    {
        var row = (CrmRecord)record;
        if (source.StartsWith("assignment:", StringComparison.Ordinal) && int.TryParse(source[11..], out var slot))
        {
            await Set<RecordProfileAssignment>().Where(x => x.RecordId == row.Id).LoadAsync(ct);
            var assignment = ChangeTracker.Entries<RecordProfileAssignment>().SingleOrDefault(e => e.State != EntityState.Deleted && e.Entity.RecordId == row.Id && e.Entity.TenantId == TenantId && e.Entity.Slot == slot)?.Entity;
            if (assignment == null) return null;
            var definition = await Set<ProfileDefinition>().FindAsync([assignment.DefinitionId], ct);
            return definition != null && definition.TenantId == TenantId ? definition.ChildNode : null;
        }
        if (source.StartsWith("social:", StringComparison.Ordinal) && row.Kind == "contact")
        {
            var property = source[7..];
            var channel = property == "OnlineCommunities" ? "online-communities" : property.EndsWith("Id") ? property[..^2].ToLowerInvariant() : property.ToLowerInvariant();
            await Set<ContactSocialProfile>().Where(x => x.RecordId == row.Id).LoadAsync(ct);
            return ChangeTracker.Entries<ContactSocialProfile>().SingleOrDefault(e => e.State != EntityState.Deleted && e.Entity.RecordId == row.Id && e.Entity.TenantId == TenantId && e.Entity.Channel == channel)?.Entity.Identifier;
        }
        if (!source.StartsWith("profile:", StringComparison.Ordinal)) throw new System.ComponentModel.DataAnnotations.ValidationException("Source field is unavailable.");
        if (row.Kind == "contact")
        {
            await Set<ContactProfile>().Where(x => x.RecordId == row.Id).LoadAsync(ct);
            var profile = ChangeTracker.Entries<ContactProfile>().SingleOrDefault(e => e.State != EntityState.Deleted && e.Entity.RecordId == row.Id && e.Entity.TenantId == TenantId)?.Entity;
            return profile == null ? null : typeof(ContactProfile).GetProperty(source[8..])!.GetValue(profile);
        }
        await Set<AccountProfile>().Where(x => x.RecordId == row.Id).LoadAsync(ct);
        var account = ChangeTracker.Entries<AccountProfile>().SingleOrDefault(e => e.State != EntityState.Deleted && e.Entity.RecordId == row.Id && e.Entity.TenantId == TenantId)?.Entity;
        return account == null ? null : typeof(AccountProfile).GetProperty(source[8..])!.GetValue(account);
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
            if (e.Entity is ProfileDefinition definition)
            {
                if (e.State == EntityState.Deleted) throw new CrmBoundaryException();
                if (string.IsNullOrWhiteSpace(definition.ParentNode) || string.IsNullOrWhiteSpace(definition.ChildNode) ||
                    definition.ParentNode.Length > 160 || definition.ChildNode.Length > 160 || definition.ParentNode.Any(char.IsControl) || definition.ChildNode.Any(char.IsControl))
                    throw new System.ComponentModel.DataAnnotations.ValidationException("Provide parent and child profile names of at most 160 characters without control characters.");
                definition.ParentNode = definition.ParentNode.Trim(); definition.ChildNode = definition.ChildNode.Trim();
                definition.NormalizedParent = definition.ParentNode.ToUpperInvariant(); definition.NormalizedChild = definition.ChildNode.ToUpperInvariant();
            }
            if (e.Entity is RecordProfileAssignment assignment)
            {
                if (e.State != EntityState.Added && (e.Property(nameof(RecordChild.RecordId)).IsModified || e.Property(nameof(RecordProfileAssignment.Slot)).IsModified)) throw new CrmBoundaryException();
                if (!await Records.AnyAsync(x => x.Id == assignment.RecordId && (x.Kind == "contact" || x.Kind == "account") && !x.IsArchived, ct)) throw new CrmBoundaryException();
                if (e.State != EntityState.Deleted)
                {
                    if (assignment.Slot is < 1 or > 5) throw new System.ComponentModel.DataAnnotations.ValidationException("Profile slots must be between 1 and 5.");
                    var selected = await Set<ProfileDefinition>().SingleOrDefaultAsync(x => x.Id == assignment.DefinitionId, ct) ?? throw new CrmBoundaryException();
                    if (selected.IsArchived && (e.State == EntityState.Added || e.Property(nameof(RecordProfileAssignment.DefinitionId)).IsModified)) throw new CrmBoundaryException();
                }
            }
            if (e.Entity is ContactProfile or ContactSocialProfile or AccountProfile)
            {
                var profile = (RecordChild)e.Entity;
                var kind = profile is AccountProfile ? "account" : "contact";
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
        var changedProfiles = ChangeTracker.Entries<RecordProfileAssignment>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Select(e => e.Entity.RecordId).Distinct().ToArray();
        foreach (var recordId in changedProfiles)
        {
            await Set<RecordProfileAssignment>().Where(x => x.RecordId == recordId).LoadAsync(ct);
            var final = ChangeTracker.Entries<RecordProfileAssignment>().Where(e => e.State != EntityState.Deleted && e.Entity.TenantId == TenantId && e.Entity.RecordId == recordId).Select(e => e.Entity).ToArray();
            if (final.Length > 5 || final.Select(x => x.Slot).Distinct().Count() != final.Length || final.Select(x => x.DefinitionId).Distinct().Count() != final.Length)
                throw new System.ComponentModel.DataAnnotations.ValidationException("Choose distinct profiles in slots 1 to 5.");
        }
    }
}
public sealed class CrmBoundaryException() : InvalidOperationException("Operation outside the active CRM boundary.");
