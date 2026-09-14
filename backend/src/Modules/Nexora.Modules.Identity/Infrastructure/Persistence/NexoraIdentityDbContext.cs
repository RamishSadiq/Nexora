using Microsoft.AspNetCore.Identity;
using Nexora.Modules.Identity.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Identity.Domain;

namespace Nexora.Modules.Identity.Infrastructure.Persistence;

public sealed partial class NexoraIdentityDbContext(DbContextOptions<NexoraIdentityDbContext> options, ITenantContext? tenantContext = null)
    : IdentityDbContext<NexoraUser, NexoraRole, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Tenant>().HasQueryFilter(x => SystemAccess || (x.Id == CurrentTenantId));
        builder.Entity<NexoraUser>().HasQueryFilter(x => SystemAccess || (x.TenantId == CurrentTenantId));
        builder.Entity<NexoraRole>().HasQueryFilter(x => SystemAccess || (x.TenantId == CurrentTenantId));
        builder.Entity<Team>().HasQueryFilter(x => SystemAccess || (x.TenantId == CurrentTenantId));
        builder.Entity<AuditEvent>().HasQueryFilter(x => SystemAccess || (CurrentTenantId != null && x.TenantId == CurrentTenantId));
        builder.Entity<TeamMember>().HasQueryFilter(x => SystemAccess || (Teams.Any(t => t.Id == x.TeamId) && Users.Any(u => u.Id == x.UserId)));
        builder.Entity<RolePermission>().HasQueryFilter(x => SystemAccess || (Roles.Any(r => r.Id == x.RoleId)));
        builder.Entity<IdentityUserRole<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId) && Roles.Any(r => r.Id == x.RoleId)));
        builder.Entity<IdentityUserClaim<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId)));
        builder.Entity<IdentityUserLogin<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId)));
        builder.Entity<IdentityUserToken<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId)));
        builder.Entity<IdentityRoleClaim<Guid>>().HasQueryFilter(x => SystemAccess || (Roles.Any(r => r.Id == x.RoleId)));


        builder.HasDefaultSchema("identity");
        builder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Slug).HasMaxLength(80);
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<NexoraUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.NormalizedEmail });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NexoraRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<Team>(entity =>
        {
            entity.ToTable("Teams");
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("TeamMembers");
            entity.HasKey(x => new { x.TeamId, x.UserId });
            entity.HasOne(x => x.Team).WithMany(x => x.Members).HasForeignKey(x => x.TeamId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(x => new { x.RoleId, x.Permission });
            entity.Property(x => x.Permission).HasMaxLength(120);
            entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("AuditEvents");
            entity.Property(x => x.EventType).HasMaxLength(120);
            entity.Property(x => x.Outcome).HasMaxLength(40);
            entity.Property(x => x.Subject).HasMaxLength(320);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
        });
    }
}
