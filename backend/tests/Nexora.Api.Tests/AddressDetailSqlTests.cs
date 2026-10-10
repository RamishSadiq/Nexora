using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class AddressDetailSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Address_details_upgrade_preserves_legacy_addresses_and_enforces_purpose_slots()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        { InitialCatalog = "NexoraAddressTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray();
            var index = Array.FindIndex(migrations, x => x.EndsWith("_AddressDetailsAndBillingSource")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var recordId = Guid.NewGuid(); var addressId = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({recordId},{identity.TenantId},'contact','Existing contact','active',0,{Guid.NewGuid()},{now},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Addresses] ([Id],[TenantId],[RecordId],[Label],[Line1],[Line2],[City],[Region],[PostalCode],[Country]) VALUES ({addressId},{identity.TenantId},{recordId},'Legacy','Original road','Original line 2','York','North','YO1 1AA','UK')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var legacy = await db.Set<Address>().SingleAsync();
            Assert.Equal("other", legacy.Purpose); Assert.Equal("Original line 2", legacy.Line2); Assert.Equal("North", legacy.Region); Assert.Null(legacy.County);
            Assert.False((await db.Records.SingleAsync()).BillingUsesMailingAddress);
            foreach (var purpose in new[] { "mailing", "billing" })
            {
                db.Add(new Address { TenantId = identity.TenantId!.Value, RecordId = recordId, Purpose = purpose, Label = purpose, Line1 = "1 Road", Line3 = "Floor 3", Line4 = "Room 4", City = "York", County = "Yorkshire", Region = "North", PostalCode = "YO1 1AA", Country = "UK", AddressType = "Office", AccountName = "Example Ltd", JobTitle = "Director" });
                await db.SaveChangesAsync();
                var duplicate = new Address { TenantId = identity.TenantId.Value, RecordId = recordId, Purpose = purpose, Label = "Duplicate", Line1 = "2 Road", City = "Town", PostalCode = "POST1", Country = "UK" };
                db.Add(duplicate); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            }
            db.Add(new Address { TenantId = identity.TenantId!.Value, RecordId = recordId, Label = "Another", Purpose = "other", Line1 = "Other road", City = "Town", PostalCode = "POST1", Country = "UK" });
            var record = await db.Records.SingleAsync(); record.BillingUsesMailingAddress = true;
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            Assert.Equal(4, await db.Set<Address>().CountAsync()); Assert.True((await db.Records.SingleAsync()).BillingUsesMailingAddress);
            var mailing = await db.Set<Address>().SingleAsync(x => x.Purpose == "mailing");
            Assert.Equal("Floor 3", mailing.Line3); Assert.Equal("Room 4", mailing.Line4); Assert.Equal("Yorkshire", mailing.County); Assert.Equal("North", mailing.Region);
            Assert.Equal("Example Ltd", mailing.AccountName); Assert.Equal("Director", mailing.JobTitle); Assert.Equal("Office", mailing.AddressType);
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty(await outside.Set<Address>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
