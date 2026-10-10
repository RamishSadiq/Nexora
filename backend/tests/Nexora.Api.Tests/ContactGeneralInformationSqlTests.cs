using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class ContactGeneralInformationSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task General_information_migration_preserves_profiles_and_persists_long_remarks()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        { InitialCatalog = "NexoraContactGeneralTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray();
            var index = Array.FindIndex(migrations, x => x.EndsWith("_ContactGeneralInformation"));
            Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var id = Guid.NewGuid(); var profileId = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({id},{identity.TenantId},'contact','Existing contact','active',0,{Guid.NewGuid()},{now},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[ContactProfiles] ([Id],[TenantId],[RecordId],[FirstName]) VALUES ({profileId},{identity.TenantId},{id},'Existing')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            Assert.False(db.Database.HasPendingModelChanges());
            var profile = await db.Set<ContactProfile>().SingleAsync();
            Assert.Equal("Existing", profile.FirstName); Assert.Null(profile.SourcePersonId); Assert.Null(profile.Remarks);
            profile.SourcePersonId = "legacy-456"; profile.ContactType = "Individual"; profile.Nationality = "British";
            profile.OldReference = "OLD-456"; profile.Region = "North"; profile.Branch = "York"; profile.Remarks = new string('a', 4000);
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            profile = await db.Set<ContactProfile>().SingleAsync();
            Assert.Equal("legacy-456", profile.SourcePersonId); Assert.Equal(4000, profile.Remarks!.Length);
            Assert.Equal("York", profile.Branch); Assert.Equal("Existing", profile.FirstName);
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid()));
            Assert.Empty(await outside.Set<ContactProfile>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
