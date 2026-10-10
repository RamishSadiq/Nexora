using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class MembershipAdministrationSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Membership_administration_upgrade_preserves_subscriptions_and_nullable_tracking()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraMembershipAdminTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new MembershipDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync();
            var product = new MembershipProduct { TenantId = identity.TenantId!.Value, Name = "Existing", Category = "Standard", Rate = 125, Currency = "GBP" }; db.Add(product); await db.SaveChangesAsync();
            var application = new MembershipApplication { TenantId = identity.TenantId.Value, ContactId = Guid.NewGuid(), ProductId = product.Id, ApplicationType = "individual", Status = "approved" }; db.Add(application); await db.SaveChangesAsync();
            var member = new MemberSubscription { TenantId = identity.TenantId.Value, ContactId = application.ContactId, ProductId = product.Id, ApplicationId = application.Id, StartsAtUtc = DateTime.UtcNow.Date, EndsAtUtc = DateTime.UtcNow.Date.AddYears(1), Rate = 125, Currency = "GBP" }; db.Add(member); await db.SaveChangesAsync();
            var memberId = member.Id; var version = member.Version; db.ChangeTracker.Clear();
            // Seed a valid subscription, remove only this migration's columns, then exercise the upgrade.
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_MembershipAdministrativeDetails")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]); await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            member = await db.Set<MemberSubscription>().SingleAsync(); Assert.Equal(memberId, member.Id); Assert.Equal(version, member.Version); Assert.Equal(125, member.Rate); Assert.Equal("active", member.Status);
            Assert.Null(member.PrimaryMembership); Assert.Null(member.PaymentProvider); Assert.Null(member.RenewalMonth); Assert.Null(member.RecordedRenewalDate); Assert.Null(member.NextRenewalDate); Assert.Null(member.PaidById); Assert.Null(member.InvoiceToAccount); Assert.Null(member.ExemptJoiningFee); Assert.Null(member.FreeMembership); Assert.Null(member.ExcludedFromLapse); Assert.False(member.IsArchived); Assert.Null(member.ArchivedAtUtc); Assert.Null(member.ArchivedByUserId); Assert.Null(member.ArchiveReason); Assert.Null(member.StatusEffectiveOn); Assert.Null(member.ResignedOn); Assert.Null(member.ResignationReason); Assert.Null(member.ReinstatedOn); Assert.Null(member.OriginalJoinDate); Assert.Null(member.LegacyMemberReference); Assert.Null(member.MembershipPackSent); Assert.Null(member.MembershipPackSentDate); Assert.Null(member.Remarks);
            member.PrimaryMembership = true; member.PaymentProvider = "gocardless"; member.RenewalMonth = 11; member.RecordedRenewalDate = new DateOnly(2001, 2, 3); member.NextRenewalDate = new DateOnly(2002, 2, 3); member.PromotionCode = "LEGACY"; member.PaymentMethod = "Bank transfer"; member.PaymentFrequency = "Annual"; member.PaidById = application.ContactId; member.PaidByName = "Recorded contact"; member.PaidByKind = "contact"; member.InvoiceToAccount = false; member.ExemptJoiningFee = true; member.FreeMembership = false; member.ExcludedFromLapse = true; member.Status = "cancelled"; member.IsArchived = true; member.ArchivedAtUtc = DateTime.UtcNow; member.ArchivedByUserId = identity.UserId; member.ArchiveReason = "Historical archive"; member.StatusEffectiveOn = new DateOnly(2001, 2, 3); member.ResignedOn = new DateOnly(2001, 2, 3); member.ResignationReason = "Historical resignation"; member.ReinstatedOn = new DateOnly(2001, 2, 4); member.OriginalJoinDate = new DateOnly(1998, 3, 4); member.LegacyMemberReference = "MEM-42"; member.MembershipPackSent = true; member.MembershipPackSentDate = new DateOnly(2001, 2, 3); member.Remarks = new string('x', 4000); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            db.Add(new MembershipHistory { TenantId = identity.TenantId!.Value, SubjectId = memberId, ActorUserId = identity.UserId!.Value, Action = "membership.resign", Reason = "Historical resignation", EffectiveOn = new DateOnly(2001, 2, 3) }); await db.SaveChangesAsync();
            Assert.Equal(new DateOnly(2001, 2, 3), await db.Set<MembershipHistory>().Where(x => x.SubjectId == memberId && x.EffectiveOn != null).MaxAsync(x => x.EffectiveOn));
            var type = new MembershipSetupValue { TenantId = identity.TenantId!.Value, Kind = "member-type", Name = "Professional" }; db.Add(type); await db.SaveChangesAsync(); db.Add(new MembershipClassificationChange { TenantId = identity.TenantId.Value, MembershipId = memberId, Kind = "member-type", DefinitionId = type.Id, NameSnapshot = type.Name, EffectiveOn = new DateOnly(2001, 2, 3), Sequence = 1, ActorUserId = identity.UserId!.Value, Reason = "Historical type" });
            var policy = new MembershipPolicy { TenantId = identity.TenantId.Value, ProductId = product.Id, JoiningFee = 25 }; db.Add(policy); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); Assert.Equal(25, (await db.Set<MembershipPolicy>().SingleAsync()).JoiningFee);
            var projected = Assert.Single((await new CrmMembershipDirectory(db).ReadAsync(application.ContactId, "contact", 1, 10, default)).Items);
            Assert.Equal("gocardless", projected.FinancialArrangement!.PaymentProvider); Assert.Equal(11, projected.FinancialArrangement.RenewalMonth); Assert.Equal(new DateOnly(2001, 2, 3), projected.FinancialArrangement.RenewalDate); Assert.Equal("Recorded contact", projected.FinancialArrangement.PaidByName); Assert.Equal("Professional", projected.Classifications.MemberType.Name); Assert.True(projected.Options!.PrimaryMembership); Assert.True(projected.Options.ExemptJoiningFee); Assert.False(projected.Options.FreeMembership); Assert.True(projected.Options.ExcludedFromLapse); Assert.True(projected.IsArchived); Assert.NotNull(projected.ArchivedAtUtc); Assert.Equal("Historical archive", projected.ArchiveReason); Assert.Equal(new DateOnly(2001, 2, 3), projected.LifecycleDates!.StatusDate); Assert.Equal("Historical resignation", projected.LifecycleDates.ResignReason); Assert.Equal(new DateOnly(2001, 2, 4), projected.LifecycleDates.ReinstatementDate); Assert.Equal(new DateOnly(2001, 2, 3), projected.LatestLifecycleEffectiveOn); Assert.Equal(new DateOnly(1998, 3, 4), projected.OriginalJoinDate); Assert.Equal("MEM-42", projected.Administration.LegacyMemberReference); Assert.True(projected.Administration.MembershipPackSent); Assert.Equal(new DateOnly(2001, 2, 3), projected.Administration.MembershipPackSentDate); Assert.Equal(4000, projected.Administration.Remarks!.Length);
            member = await db.Set<MemberSubscription>().SingleAsync(); member.MembershipPackSent = false; member.MembershipPackSentDate = null; await db.SaveChangesAsync(); db.ChangeTracker.Clear(); Assert.False((await db.Set<MemberSubscription>().SingleAsync()).MembershipPackSent);
            member = await db.Set<MemberSubscription>().SingleAsync(); member.IsArchived = false; member.ArchivedAtUtc = null; member.ArchivedByUserId = null; member.ArchiveReason = null; await db.SaveChangesAsync(); db.ChangeTracker.Clear(); Assert.False((await db.Set<MemberSubscription>().SingleAsync()).IsArchived); Assert.Equal("cancelled", (await db.Set<MemberSubscription>().SingleAsync()).Status);
            var category = new MembershipSetupValue { TenantId = identity.TenantId!.Value, Kind = "member-category", Name = "Timeline category" }; db.Add(category); await db.SaveChangesAsync();
            for (var i = 0; i < 30; i++) db.Add(new MembershipClassificationChange { TenantId = identity.TenantId.Value, MembershipId = memberId, Kind = "category", DefinitionId = category.Id, NameSnapshot = "Category " + i, EffectiveOn = new DateOnly(2000, 1, 1).AddDays(i), Sequence = i + 1, ActorUserId = identity.UserId!.Value, Reason = "SQL timeline" });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var timeline = await Nexora.Modules.Membership.MembershipModule.ReadTimelineAsync(memberId, "2", "category", db, default); Assert.Equal(30, timeline.Total); Assert.Equal(5, timeline.Items.Count); Assert.Equal(new DateOnly(2000, 1, 6), timeline.Items[0].CeasedDate); Assert.Equal(1, timeline.Items[0].Days);
            await using var outside = new MembershipDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty(await outside.Set<MemberSubscription>().ToListAsync()); await Assert.ThrowsAsync<KeyNotFoundException>(() => Nexora.Modules.Membership.MembershipModule.ReadTimelineAsync(memberId, "1", null, outside, default));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
