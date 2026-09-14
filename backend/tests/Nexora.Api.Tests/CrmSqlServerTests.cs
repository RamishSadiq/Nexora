using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
using Xunit.Abstractions;

namespace Nexora.Api.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")))
            Skip = "Set NEXORA_TEST_SQLSERVER to run isolated SQL Server migration and volume checks.";
    }
}

public sealed class CrmSqlServerTests(ITestOutputHelper output)
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    [SqlServerFact]
    public async Task Migration_is_idempotent_and_tenant_queries_work_at_10000_records()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        { InitialCatalog = "NexoraCrmTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new CrmDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();
            db.Records.AddRange(Enumerable.Range(0, 10000).Select(i => new CrmRecord
            { TenantId = identity.TenantId!.Value, Name = $"Volume contact {i:D5}", Kind = "contact" }));
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var timings = new List<double>();
            for (var i = 0; i < 20; i++)
            {
                var watch = Stopwatch.StartNew();
                var records = await db.Records.AsNoTracking().Where(x => x.Kind == "contact" && !x.IsArchived && x.Name.Contains("contact"))
                    .OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(i * 20).Take(20).ToListAsync();
                Assert.Equal(20, records.Count); timings.Add(watch.Elapsed.TotalMilliseconds);
            }
            timings.Sort(); var p95 = timings[18];
            output.WriteLine($"SQL Server: 10,000 records; 20 filtered pages; p95 {p95:F1} ms; maximum {timings[^1]:F1} ms.");
            Assert.True(p95 < 2500, $"Filtered-page p95 was {p95:F1} ms.");
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid()));
            Assert.Empty(await outside.Records.ToListAsync());
            var own = new CrmRecord { TenantId = identity.TenantId!.Value, Name = "Owned" };
            db.Add(own); await db.SaveChangesAsync();
            db.Add(new RecordRelationship { TenantId = identity.TenantId.Value, RecordId = own.Id, TargetRecordId = Guid.NewGuid(), Label = "Invalid" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            // The catalog is generated above solely for this test; never deletes a configured user database.
            await db.Database.EnsureDeletedAsync();
        }
    }
}
