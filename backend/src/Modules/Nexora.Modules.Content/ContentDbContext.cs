using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;

public sealed class ContentDbContext(DbContextOptions<ContentDbContext> options, IRequestIdentity identity) : DbContext(options)
{
    private Guid? TenantId => identity.TenantId;
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageVersion> Versions => Set<PageVersion>();
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("content");
        Configure<Site>(b, "Sites"); Configure<Page>(b, "Pages");
        Configure<PortalConfiguration>(b, "PortalConfigurations");
        b.Entity<PortalConfiguration>().HasIndex(x => x.TenantId).IsUnique();
        b.Entity<PortalConfiguration>().Property(x => x.DraftJson).HasMaxLength(100000);
        b.Entity<PortalConfiguration>().Property(x => x.PublishedJson).HasMaxLength(100000);
        Configure<PageVersion>(b, "PageVersions"); Configure<Publication>(b, "Publications");
        Configure<MediaAsset>(b, "MediaAssets"); Configure<PageRedirect>(b, "PageRedirects"); Configure<ScheduledPublication>(b, "ScheduledPublications");
        b.Entity<Site>().Property(x => x.BrandColor).HasMaxLength(7);
        b.Entity<Page>().Property(x => x.NavigationLabel).HasMaxLength(80);
        b.Entity<PageRedirect>().Property(x => x.Slug).HasMaxLength(100);
        b.Entity<PageRedirect>().HasIndex(x => new { x.TenantId, x.SiteId, x.Slug }).IsUnique();
        b.Entity<PageRedirect>().HasOne<Page>().WithMany().HasForeignKey(x => new { x.TenantId, x.PageId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MediaAsset>().HasOne<Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MediaAsset>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<MediaAsset>().Property(x => x.ContentType).HasMaxLength(30);
        b.Entity<MediaAsset>().Property(x => x.ScanStatus).HasMaxLength(20);
        b.Entity<MediaAsset>().Property(x => x.Content).HasMaxLength(5 * 1024 * 1024);
        b.Entity<ScheduledPublication>().Property(x => x.Status).HasMaxLength(20);
        b.Entity<ScheduledPublication>().Property(x => x.Failure).HasMaxLength(300);
        b.Entity<ScheduledPublication>().HasIndex(x => new { x.TenantId, x.PageId }).IsUnique().HasFilter("[Status] = 'pending'");
        b.Entity<ScheduledPublication>().HasOne<Page>().WithMany().HasForeignKey(x => new { x.TenantId, x.PageId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Site>().HasIndex(x => x.TenantId).IsUnique();
        b.Entity<Site>().Property(x => x.Name).HasMaxLength(100);
        b.Entity<Site>().Property(x => x.Tagline).HasMaxLength(200);
        b.Entity<Page>().Property(x => x.Slug).HasMaxLength(100);
        b.Entity<Page>().HasIndex(x => new { x.TenantId, x.SiteId, x.Slug }).IsUnique();
        b.Entity<Page>().HasOne<Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PageVersion>().HasOne<Page>().WithMany().HasForeignKey(x => new { x.TenantId, x.PageId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Publication>().HasOne<Page>().WithMany().HasForeignKey(x => new { x.TenantId, x.PageId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PageVersion>().Property(x => x.Title).HasMaxLength(120);
        b.Entity<PageVersion>().Property(x => x.Description).HasMaxLength(300);
        b.Entity<PageVersion>().Property(x => x.BlocksJson).HasMaxLength(40000);
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(DateTime)))
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
    }
    private void Configure<T>(ModelBuilder b, string name) where T : TenantEntity
    {
        var e = b.Entity<T>(); e.HasBaseType((Type?)null); e.ToTable(name); e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.TenantId, x.Id }); e.HasQueryFilter(x => x.TenantId == TenantId);
        e.Property(x => x.Version).IsConcurrencyToken();
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray())
        {
            if (TenantId == null || identity.UserId == null || entry.Entity.TenantId != TenantId) throw new TenantBoundaryException();
            if (entry.Entity is IAppendOnly && entry.State != EntityState.Added) throw new TenantBoundaryException();
            if (entry.State != EntityState.Added)
            {
                var stored = await entry.GetDatabaseValuesAsync(ct);
                if (stored == null || !Equals(stored["TenantId"], TenantId)) throw new TenantBoundaryException();
                entry.Entity.CreatedAtUtc = (DateTime)stored["CreatedAtUtc"]!;
                entry.Entity.Version = Guid.NewGuid(); entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }
}

public sealed class ContentDesignTimeFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    private sealed class EmptyIdentity : IRequestIdentity { public Guid? TenantId => null; public Guid? UserId => null; }
    public ContentDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ContentDbContext>().UseSqlServer(
        Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING") ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True",
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "content")).Options, new EmptyIdentity());
}
