using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class AccountGeneralInformationSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Account_general_information_upgrade_preserves_existing_names_and_unknown_counts()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraAccountGeneralTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_AccountGeneralInformation")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var id = Guid.NewGuid(); var profileId = Guid.NewGuid(); var version = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc],[BillingUsesMailingAddress]) VALUES ({id},{identity.TenantId},'account','Existing Account','active',0,{version},{now},{now},0)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[AccountProfiles] ([Id],[TenantId],[RecordId],[FullName],[TradingAs]) VALUES ({profileId},{identity.TenantId},{id},'Existing full name','Existing trading name')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var profile = await db.Set<AccountProfile>().SingleAsync(); Assert.Equal("Existing full name", profile.FullName); Assert.Equal("Existing trading name", profile.TradingAs); Assert.Null(profile.SourceAccountId); Assert.Null(profile.NumberOfEmployees); Assert.Null(profile.TradingStatus); Assert.Equal(version, (await db.Records.SingleAsync()).Version);
            profile.SourceAccountId = "LEGACY-42"; profile.TradingStatus = "Dormant"; profile.AccountType = "Association"; profile.IndustrySector = "Engineering"; profile.Venue = "Recorded source venue"; profile.MarketingSource = "Campaign"; profile.TurnoverRange = "1–5 million"; profile.NumberOfEmployees = 0; profile.EmployeesRange = "0–10"; profile.Region = "South"; profile.Branch = "London"; profile.OldReference = "OLD-42";
            await db.SaveChangesAsync(); db.ChangeTracker.Clear(); profile = await db.Set<AccountProfile>().SingleAsync(); Assert.Equal(0, profile.NumberOfEmployees); Assert.Equal("LEGACY-42", profile.SourceAccountId); Assert.Equal("OLD-42", profile.OldReference); Assert.Equal("Dormant", profile.TradingStatus); Assert.Equal("active", (await db.Records.SingleAsync()).Status);
            profile.NumberOfEmployees = null; await db.SaveChangesAsync(); db.ChangeTracker.Clear(); Assert.Null((await db.Set<AccountProfile>().SingleAsync()).NumberOfEmployees);
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty(await outside.Set<AccountProfile>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
