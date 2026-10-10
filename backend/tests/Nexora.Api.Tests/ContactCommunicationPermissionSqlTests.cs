using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class ContactCommunicationPermissionSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Permission_upgrade_preserves_unknown_values_and_suppresses_marketing_without_overriding_consent()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        { InitialCatalog = "NexoraPermissionsTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray();
            var index = Array.FindIndex(migrations, x => x.EndsWith("_ContactCommunicationPermissions")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var recordId = Guid.NewGuid(); var profileId = Guid.NewGuid(); var methodId = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({recordId},{identity.TenantId},'contact','Existing contact','active',0,{Guid.NewGuid()},{now},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[ContactProfiles] ([Id],[TenantId],[RecordId],[FirstName]) VALUES ({profileId},{identity.TenantId},{recordId},'Existing')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[CommunicationMethods] ([Id],[TenantId],[RecordId],[Kind],[Value],[Consent],[ConsentEvidence]) VALUES ({methodId},{identity.TenantId},{recordId},'email','existing@example.test','granted','Original consent')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var profile = await db.Set<ContactProfile>().SingleAsync();
            Assert.Equal("Existing", profile.FirstName); Assert.Null(profile.ThirdPartyEmailPermission); Assert.Null(profile.DisclosureDataToThirdParty); Assert.Null(profile.StopAllCommunication); Assert.Null(profile.BouncedInvalidEmailAddress);
            var services = new ServiceCollection(); services.AddNexoraCrm(new ConfigurationBuilder().Build()); services.AddSingleton(db);
            using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope(); var directory = scope.ServiceProvider.GetRequiredService<ICrmDirectory>();
            Assert.Equal("existing@example.test", await directory.MarketingEmailAsync(recordId, default));
            profile.ThirdPartyEmailPermission = false; profile.DisclosureDataToThirdParty = false; profile.TypeOfThirdParty = "Research partner"; profile.ThirdPartyDisclosureConsentDate = new DateOnly(2001, 2, 3);
            await db.SaveChangesAsync(); Assert.Equal("existing@example.test", await directory.MarketingEmailAsync(recordId, default));
            profile.StopAllCommunication = true; profile.DateStopAllCommunication = new DateOnly(2002, 3, 4); await db.SaveChangesAsync(); Assert.Null(await directory.MarketingEmailAsync(recordId, default));
            profile.StopAllCommunication = false; await db.SaveChangesAsync(); Assert.Equal("existing@example.test", await directory.MarketingEmailAsync(recordId, default));
            profile.BouncedInvalidEmailAddress = true; profile.DateEmailBounced = new DateOnly(2003, 4, 5); await db.SaveChangesAsync(); Assert.Null(await directory.MarketingEmailAsync(recordId, default));
            profile.BouncedInvalidEmailAddress = null; profile.ThirdPartyEmailPermission = true; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            profile = await db.Set<ContactProfile>().SingleAsync(); Assert.True(profile.ThirdPartyEmailPermission); Assert.False(profile.StopAllCommunication);
            Assert.Equal(new DateOnly(2001, 2, 3), profile.ThirdPartyDisclosureConsentDate); Assert.Equal(new DateOnly(2002, 3, 4), profile.DateStopAllCommunication); Assert.Equal(new DateOnly(2003, 4, 5), profile.DateEmailBounced);
            Assert.Null(profile.BouncedInvalidEmailAddress); Assert.Equal("Research partner", profile.TypeOfThirdParty);
            var email = await db.Set<CommunicationMethod>().SingleAsync(); Assert.Equal("granted", email.Consent); Assert.Equal("Original consent", email.ConsentEvidence);
            email.Consent = "unknown"; await db.SaveChangesAsync(); Assert.Null(await directory.MarketingEmailAsync(recordId, default));
            email.Consent = "denied"; email.ConsentEvidence = "Opted out"; await db.SaveChangesAsync(); Assert.Null(await directory.MarketingEmailAsync(recordId, default));
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty(await outside.Set<ContactProfile>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
