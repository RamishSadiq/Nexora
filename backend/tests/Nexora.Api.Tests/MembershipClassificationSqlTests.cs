using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Persistence;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class MembershipClassificationSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Classification_upgrade_preserves_subscriptions_and_enforces_immutable_tenant_bound_history()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraClassificationTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new MembershipDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync();
            var product = new MembershipProduct { TenantId = identity.TenantId!.Value, Name = "Existing", Category = "Product category", Rate = 125, Currency = "GBP" }; db.Add(product); await db.SaveChangesAsync();
            var application = new MembershipApplication { TenantId = identity.TenantId.Value, ContactId = Guid.NewGuid(), ProductId = product.Id, ApplicationType = "individual", Status = "approved" }; db.Add(application); await db.SaveChangesAsync();
            var member = new MemberSubscription { TenantId = identity.TenantId.Value, ContactId = application.ContactId, ProductId = product.Id, ApplicationId = application.Id, StartsAtUtc = DateTime.UtcNow.Date, EndsAtUtc = DateTime.UtcNow.Date.AddYears(1), Rate = 125, LegacyMemberReference = "MEM-42", Remarks = "Preserve remarks" }; db.Add(member); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_MembershipClassificationHistory")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]); await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            member = await db.Set<MemberSubscription>().SingleAsync(); Assert.Equal("MEM-42", member.LegacyMemberReference); Assert.Equal("Preserve remarks", member.Remarks); Assert.Equal(125, member.Rate); Assert.Empty(await db.Set<MembershipClassificationChange>().ToListAsync());
            var a = new MembershipSetupValue { TenantId = identity.TenantId.Value, Kind = "member-category", Name = "Gold" }; var b = new MembershipSetupValue { TenantId = identity.TenantId.Value, Kind = "member-category", Name = "Silver" };
            var band = new MembershipSetupValue { TenantId = identity.TenantId.Value, Kind = "member-subscription-band", Name = "Band A" }; db.AddRange(a, b, band); await db.SaveChangesAsync();
            for (var sequence = 1; sequence <= 27; sequence++) db.Add(new MembershipClassificationChange { TenantId = identity.TenantId.Value, MembershipId = member.Id, Kind = "category", DefinitionId = sequence % 2 == 0 ? b.Id : a.Id, NameSnapshot = sequence % 2 == 0 ? "Silver" : "Gold", EffectiveOn = new DateOnly(2001, 2, 3), Sequence = sequence, ActorUserId = identity.UserId!.Value, Reason = "Same-day correction " + sequence });
            db.Add(new MembershipClassificationChange { TenantId = identity.TenantId.Value, MembershipId = member.Id, Kind = "subscription-band", DefinitionId = band.Id, NameSnapshot = "Band A", EffectiveOn = new DateOnly(2001, 2, 3), Sequence = 1, ActorUserId = identity.UserId!.Value, Reason = "Assigned" });
            db.Add(new MembershipClassificationChange { TenantId = identity.TenantId.Value, MembershipId = member.Id, Kind = "subscription-band", EffectiveOn = new DateOnly(2001, 2, 4), Sequence = 2, ActorUserId = identity.UserId.Value, Reason = "Cleared" });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var projected = Assert.Single((await new CrmMembershipDirectory(db).ReadAsync(application.ContactId, "contact", 1, 10, default)).Items);
            Assert.Equal("Gold", projected.Classifications.Category.Name); Assert.Equal(new DateOnly(2001, 2, 3), projected.Classifications.Category.EffectiveOn); Assert.Null(projected.Classifications.SubscriptionBand.Name); Assert.Equal(new DateOnly(2001, 2, 4), projected.Classifications.SubscriptionBand.EffectiveOn);
            var history = await db.Set<MembershipClassificationChange>().Where(x => x.MembershipId == member.Id).OrderByDescending(x => x.EffectiveOn).ThenByDescending(x => x.Sequence).Skip(25).Take(25).ToListAsync(); Assert.Equal(4, history.Count);
            var first = await db.Set<MembershipClassificationChange>().FirstAsync(); first.Reason = "Rewrite"; await Assert.ThrowsAsync<TenantBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            db.Remove(await db.Set<MembershipClassificationChange>().FirstAsync()); await Assert.ThrowsAsync<TenantBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [membership].[ClassificationChanges] ([Id],[TenantId],[Version],[CreatedAtUtc],[UpdatedAtUtc],[MembershipId],[Kind],[DefinitionId],[NameSnapshot],[EffectiveOn],[Sequence],[ActorUserId],[Reason]) VALUES ({Guid.NewGuid()},{identity.TenantId},{Guid.NewGuid()},{DateTime.UtcNow},{DateTime.UtcNow},{member.Id},'category',{a.Id},'Duplicate',{new DateOnly(2001,2,3)},1,{identity.UserId},'Duplicate')"));
            var outsider = new Identity(Guid.NewGuid(), Guid.NewGuid()); Guid foreignId;
            await using (var outside = new MembershipDbContext(options, outsider))
            {
                var foreign = new MembershipSetupValue { TenantId = outsider.TenantId!.Value, Kind = "member-category", Name = "Foreign" }; outside.Add(foreign); await outside.SaveChangesAsync(); foreignId = foreign.Id;
                Assert.Empty(await outside.Set<MembershipClassificationChange>().ToListAsync()); Assert.Empty((await new CrmMembershipDirectory(outside).ReadAsync(application.ContactId, "contact", 1, 10, default)).Items);
            }
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [membership].[ClassificationChanges] ([Id],[TenantId],[Version],[CreatedAtUtc],[UpdatedAtUtc],[MembershipId],[Kind],[DefinitionId],[NameSnapshot],[EffectiveOn],[Sequence],[ActorUserId],[Reason]) VALUES ({Guid.NewGuid()},{identity.TenantId},{Guid.NewGuid()},{DateTime.UtcNow},{DateTime.UtcNow},{member.Id},'category',{foreignId},'Foreign',{new DateOnly(2001,2,4)},28,{identity.UserId},'Foreign')"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [membership].[SetupValues] WHERE [Id] = {a.Id}"));
            Assert.Equal(29, await db.Set<MembershipClassificationChange>().CountAsync()); Assert.Equal(125, (await db.Set<MemberSubscription>().SingleAsync()).Rate);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
