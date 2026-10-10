using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class MembershipRejoinSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    [SqlServerFact]
    public async Task Rejoin_migration_preserves_existing_members_and_enforces_tenant_lineage_and_single_successor()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraRejoinTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new MembershipDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync();
            var product = new MembershipProduct { TenantId = identity.TenantId!.Value, Name = "Rejoin SQL", Category = "Standard", Rate = 125 }; db.Add(product); await db.SaveChangesAsync();
            var application = new MembershipApplication { TenantId = identity.TenantId.Value, ContactId = Guid.NewGuid(), ProductId = product.Id, Status = "approved" }; db.Add(application); await db.SaveChangesAsync();
            var previous = new MemberSubscription { TenantId = identity.TenantId.Value, ApplicationId = application.Id, ContactId = application.ContactId, ProductId = product.Id, Status = "cancelled", OriginalJoinDate = new DateOnly(1998, 3, 4), StartsAtUtc = new DateTime(1998, 3, 4), EndsAtUtc = new DateTime(1999, 3, 4), Rate = 125 }; db.Add(previous); await db.SaveChangesAsync();
            var version = previous.Version; var previousId = previous.Id; db.ChangeTracker.Clear();
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_MembershipRejoinLineage")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]); await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            previous = await db.Set<MemberSubscription>().SingleAsync(); Assert.Equal(previousId, previous.Id); Assert.Equal(version, previous.Version); Assert.Null(previous.PreviousMembershipId); Assert.Null(previous.RejoinedOn); Assert.Equal(new DateOnly(1998, 3, 4), previous.OriginalJoinDate);
            var nextApplication = new MembershipApplication { TenantId = identity.TenantId.Value, ContactId = application.ContactId, ProductId = product.Id, PreviousMembershipId = previous.Id, Status = "approved" }; db.Add(nextApplication); await db.SaveChangesAsync();
            var next = new MemberSubscription { TenantId = identity.TenantId.Value, ContactId = application.ContactId, ProductId = product.Id, ApplicationId = nextApplication.Id, PreviousMembershipId = previous.Id, RejoinedOn = new DateOnly(2001, 2, 3), OriginalJoinDate = previous.OriginalJoinDate, StartsAtUtc = new DateTime(2001, 2, 3), EndsAtUtc = new DateTime(2002, 2, 3), Status = "cancelled", Rate = 125 }; db.Add(next);
            db.Add(new MembershipHistory { TenantId = identity.TenantId.Value, SubjectId = next.Id, ActorUserId = identity.UserId!.Value, Action = "membership.rejoin", Reason = "Rejoin approved", EffectiveOn = next.RejoinedOn }); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var candidates = await db.Set<MemberSubscription>().Where(x => x.ContactId == application.ContactId && x.AccountId == null && (x.Status == "cancelled" || x.Status == "lapsed") && !db.Set<MemberSubscription>().Any(y => y.PreviousMembershipId == x.Id)).ToListAsync(); Assert.Equal(next.Id, Assert.Single(candidates).Id);
            var projection = (await new CrmMembershipDirectory(db).ReadAsync(application.ContactId, "contact", 1, 10, default)).Items.Single(x => x.Id == next.Id); Assert.Equal(previous.Id, projection.PreviousMembershipId); Assert.Equal(new DateOnly(2001, 2, 3), projection.RejoinedOn); Assert.Equal("membership.rejoin", projection.LatestLifecycleAction);
            var duplicateApplication = new MembershipApplication { TenantId = identity.TenantId.Value, ContactId = application.ContactId, ProductId = product.Id }; db.Add(duplicateApplication); await db.SaveChangesAsync();
            db.Add(new MemberSubscription { TenantId = identity.TenantId.Value, ContactId = application.ContactId, ProductId = product.Id, ApplicationId = duplicateApplication.Id, Status = "cancelled", PreviousMembershipId = previous.Id });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var outsideIdentity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var outside = new MembershipDbContext(options, outsideIdentity); Assert.Empty(await outside.Set<MemberSubscription>().ToListAsync());
            var outsideProduct = new MembershipProduct { TenantId = outsideIdentity.TenantId!.Value, Name = "Outside", Category = "Standard" }; outside.Add(outsideProduct); await outside.SaveChangesAsync();
            var outsideApplication = new MembershipApplication { TenantId = outsideIdentity.TenantId.Value, ProductId = outsideProduct.Id, ContactId = Guid.NewGuid() }; outside.Add(outsideApplication); await outside.SaveChangesAsync();
            var outsideMember = new MemberSubscription { TenantId = outsideIdentity.TenantId.Value, ProductId = outsideProduct.Id, ApplicationId = outsideApplication.Id, ContactId = outsideApplication.ContactId, Status = "cancelled" }; outside.Add(outsideMember); await outside.SaveChangesAsync();
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [membership].[Applications] SET [PreviousMembershipId] = {outsideMember.Id} WHERE [Id] = {nextApplication.Id}"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [membership].[Subscriptions] SET [PreviousMembershipId] = {outsideMember.Id} WHERE [Id] = {next.Id}"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [membership].[Subscriptions] WHERE [Id] = {previous.Id}"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
