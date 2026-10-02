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
        TenantTable<PortalApplicationDraft>(b,"PortalApplicationDrafts");
        TenantTable<ApplicationEvidence>(b, "ApplicationEvidence");
        b.Entity<ApplicationEvidence>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<ApplicationEvidence>().Property(x => x.Sha256).HasMaxLength(64);
        b.Entity<ApplicationEvidence>().Property(x => x.ScanStatus).HasMaxLength(20);
        b.Entity<ApplicationEvidence>().Property(x => x.Content).HasMaxLength(5 * 1024 * 1024);
        b.Entity<ApplicationEvidence>().HasOne<PortalApplicationDraft>().WithMany().HasForeignKey(x => new { x.TenantId, x.DraftId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PortalApplicationDraft>().Property(x => x.Notes).HasMaxLength(4000);
        b.Entity<PortalApplicationDraft>().Property(x => x.Terms).HasMaxLength(4000);
        b.Entity<PortalApplicationDraft>().HasOne<MembershipProduct>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PortalApplicationDraft>().HasIndex(x => new { x.TenantId, x.ContactId });
        b.Entity<PortalApplicationDraft>().HasIndex(x => new { x.TenantId, x.ContactId, x.ProductId }).IsUnique().HasFilter("[ApplicationId] IS NULL AND [AccountId] IS NULL");
        b.Entity<PortalApplicationDraft>().HasIndex(x => new { x.TenantId, x.ContactId, x.AccountId, x.ProductId }).IsUnique().HasFilter("[ApplicationId] IS NULL AND [AccountId] IS NOT NULL");
        b.Entity<MembershipProduct>().Property(x => x.PortalTerms).HasMaxLength(4000);
        TenantTable<MembershipPolicy>(b,"Policies");
        TenantTable<MembershipReview>(b,"Reviews");
        TenantTable<MembershipCertificate>(b,"Certificates");
        TenantTable<MembershipCommunication>(b,"Communications").Property(x => x.Body).HasMaxLength(4000);
        b.Entity<MembershipReview>().Property(x => x.Evidence).HasMaxLength(2000);
        b.Entity<MembershipPolicy>().HasIndex(x => new { x.TenantId, x.ProductId }).IsUnique();
        b.Entity<MembershipPolicy>().HasOne<MembershipProduct>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipReview>().HasIndex(x => new { x.TenantId, x.MembershipId, x.TermEndUtc }).IsUnique();
        b.Entity<MembershipCertificate>().HasIndex(x => new { x.TenantId, x.MembershipId, x.EndsAtUtc }).IsUnique();
        b.Entity<MembershipCommunication>().HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique();
        b.Entity<MembershipReview>().HasOne<MemberSubscription>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipCertificate>().HasOne<MemberSubscription>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipCommunication>().HasOne<MemberSubscription>().WithMany().HasForeignKey(x => new { x.TenantId, x.MembershipId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        TenantTable<MembershipProduct>(b,"Products").Property(x => x.Name).HasMaxLength(160);
        TenantTable<MembershipApplication>(b,"Applications");
        TenantTable<MemberSubscription>(b,"Subscriptions");
        TenantTable<MembershipHistory>(b,"History");
        TenantTable<PortalNotification>(b, "PortalNotifications").HasIndex(x => new { x.TenantId, x.RecipientUserId, x.CreatedAtUtc });
        b.Entity<PortalNotification>().Property(x => x.Kind).HasMaxLength(80);
        b.Entity<PortalNotification>().Property(x => x.Subject).HasMaxLength(200);
        b.Entity<PortalNotification>().Property(x => x.Body).HasMaxLength(2000);
        b.Entity<PortalNotification>().Property(x => x.DeliveryStatus).HasMaxLength(20);
        b.Entity<MembershipApplication>().HasOne<MembershipProduct>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MemberSubscription>().HasOne<MembershipProduct>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipApplication>().Property(x => x.AccountName).HasMaxLength(160);
        b.Entity<MembershipApplication>().Property(x => x.ApplicationType).HasMaxLength(40);
        b.Entity<MembershipApplication>().Property(x => x.ApplicantRole).HasMaxLength(80);
        b.Entity<MembershipApplication>().Property(x => x.ApplicantNotes).HasMaxLength(4000);
        b.Entity<MemberSubscription>().Property(x => x.AccountName).HasMaxLength(160);
        b.Entity<MemberSubscription>().HasOne<MembershipApplication>().WithMany().HasForeignKey(x => new { x.TenantId, x.ApplicationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MemberSubscription>().HasIndex(x => new { x.TenantId, x.ApplicationId }).IsUnique();
        b.Entity<MemberSubscription>().HasIndex(x => new { x.TenantId, x.ContactId })
            .IsUnique().HasFilter("[AccountId] IS NULL AND [Status] = 'active'");
        b.Entity<MemberSubscription>().HasIndex(x => new { x.TenantId, x.AccountId })
            .IsUnique().HasFilter("[AccountId] IS NOT NULL AND [Status] = 'active'");
        b.Entity<MembershipHistory>().HasIndex(x => new { x.TenantId, x.SubjectId, x.CreatedAtUtc });
        TenantTable<MembershipSetupValue>(b, "SetupValues");
        b.Entity<MembershipSetupValue>().Property(x => x.Kind).HasMaxLength(40);
        b.Entity<MembershipSetupValue>().Property(x => x.Name).HasMaxLength(160);
        b.Entity<MembershipSetupValue>().Property(x => x.Description).HasMaxLength(2000);
        b.Entity<MembershipSetupValue>().HasIndex(x => new { x.TenantId, x.Kind, x.Name }).IsUnique();
        ConfigureColumns(b);
    }
}
