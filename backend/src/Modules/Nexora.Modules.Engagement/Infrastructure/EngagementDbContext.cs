using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Engagement.Domain;
namespace Nexora.Modules.Engagement.Infrastructure;
public sealed class EngagementDbContext(DbContextOptions<EngagementDbContext> options,IRequestIdentity identity):TenantDbContext(options,identity)
{
 protected override void OnModelCreating(ModelBuilder b)
 {
  b.HasDefaultSchema("engagement");TenantTable<Community>(b,"Communities");TenantTable<CommunityMember>(b,"Members");TenantTable<CommunityMeeting>(b,"Meetings");TenantTable<Campaign>(b,"Campaigns");TenantTable<CampaignRecipient>(b,"Recipients");TenantTable<Fund>(b,"Funds");TenantTable<Contribution>(b,"Contributions");TenantTable<EngagementHistory>(b,"History");
  b.Entity<CommunityMember>().HasOne<Community>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CommunityId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<CommunityMeeting>().HasOne<Community>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CommunityId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Campaign>().HasOne<Community>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CommunityId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<CampaignRecipient>().HasOne<Campaign>().WithMany().HasForeignKey(x=>new{x.TenantId,x.CampaignId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Contribution>().HasOne<Fund>().WithMany().HasForeignKey(x=>new{x.TenantId,x.FundId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.Entity<CommunityMember>().HasIndex(x=>new{x.TenantId,x.CommunityId,x.ContactId}).IsUnique();b.Entity<CampaignRecipient>().HasIndex(x=>new{x.TenantId,x.CampaignId,x.ContactId}).IsUnique();
  b.Entity<Contribution>().Property(x=>x.Reference).HasMaxLength(100);b.Entity<Contribution>().HasIndex(x=>new{x.TenantId,x.Reference}).IsUnique();ConfigureColumns(b);
 }
}
