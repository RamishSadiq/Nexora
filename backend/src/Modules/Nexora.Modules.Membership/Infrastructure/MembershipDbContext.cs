using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
namespace Nexora.Modules.Membership.Infrastructure;
public sealed class MembershipDbContext(DbContextOptions<MembershipDbContext> options, IRequestIdentity identity) : TenantDbContext(options, identity)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("membership");
        TenantTable<MembershipProduct>(b,"Products").Property(x => x.Name).HasMaxLength(160);
        TenantTable<MembershipApplication>(b,"Applications");
        TenantTable<MemberSubscription>(b,"Subscriptions");
        TenantTable<MembershipHistory>(b,"History");
        b.Entity<MembershipApplication>().HasOne<MembershipProduct>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MemberSubscription>().HasOne<MembershipProduct>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MemberSubscription>().HasOne<MembershipApplication>().WithMany().HasForeignKey(x => new { x.TenantId, x.ApplicationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MemberSubscription>().HasIndex(x => new { x.TenantId, x.ApplicationId }).IsUnique();
        b.Entity<MemberSubscription>().HasIndex(x => new { x.TenantId, x.ContactId, x.ProductId }).IsUnique().HasFilter("Status = 'active'");
        b.Entity<MembershipHistory>().HasIndex(x => new { x.TenantId, x.SubjectId, x.CreatedAtUtc });
        ConfigureColumns(b);
    }
}
