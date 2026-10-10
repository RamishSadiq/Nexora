using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Crm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nexora.Api.Tests;

public sealed class CommunicationPrioritySqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Communication_priority_upgrade_preserves_entries_and_enforces_unique_primary_and_secondary()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        { InitialCatalog = "NexoraCommunicationTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray();
            var index = Array.FindIndex(migrations, x => x.EndsWith("_CommunicationPriority")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var recordId = Guid.NewGuid(); var methodId = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({recordId},{identity.TenantId},'contact','Existing contact','active',0,{Guid.NewGuid()},{now},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[CommunicationMethods] ([Id],[TenantId],[RecordId],[Kind],[Value],[Consent],[ConsentEvidence]) VALUES ({methodId},{identity.TenantId},{recordId},'email','existing@example.test','granted','Original consent')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var method = await db.Set<CommunicationMethod>().SingleAsync();
            Assert.Equal("other", method.Priority); Assert.Equal("Original consent", method.ConsentEvidence);
            foreach (var priority in new[] { "primary", "secondary" })
            {
                var first = new CommunicationMethod { TenantId = identity.TenantId!.Value, RecordId = recordId, Kind = "email", Priority = priority, Value = priority + "@example.test", Consent = "granted", ConsentEvidence = "Requested updates" };
                db.Add(first); await db.SaveChangesAsync();
                var duplicate = new CommunicationMethod { TenantId = identity.TenantId.Value, RecordId = recordId, Kind = "email", Priority = priority, Value = "duplicate@example.test" };
                db.Add(duplicate); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            }
            db.AddRange(new CommunicationMethod { TenantId = identity.TenantId!.Value, RecordId = recordId, Kind = "email", Priority = "other", Value = "other@example.test" },
                new CommunicationMethod { TenantId = identity.TenantId.Value, RecordId = recordId, Kind = "mobile", Priority = "primary", Value = "0123456789" });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear(); Assert.Equal(5, await db.Set<CommunicationMethod>().CountAsync());
            var services = new ServiceCollection(); services.AddNexoraCrm(new ConfigurationBuilder().Build()); services.AddSingleton(db);
            using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
            var directory = scope.ServiceProvider.GetRequiredService<ICrmDirectory>();
            Assert.Equal("primary@example.test", await directory.MarketingEmailAsync(recordId, default));
            db.Remove(await db.Set<CommunicationMethod>().SingleAsync(x => x.Kind == "email" && x.Priority == "primary")); await db.SaveChangesAsync();
            Assert.Equal("secondary@example.test", await directory.MarketingEmailAsync(recordId, default));
            db.Add(new CommunicationMethod { TenantId = identity.TenantId.Value, RecordId = recordId, Kind = "email", Priority = "other", Value = "denied@example.test", Consent = "denied", ConsentEvidence = "Opted out" });
            await db.SaveChangesAsync(); Assert.Null(await directory.MarketingEmailAsync(recordId, default));
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty(await outside.Set<CommunicationMethod>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
