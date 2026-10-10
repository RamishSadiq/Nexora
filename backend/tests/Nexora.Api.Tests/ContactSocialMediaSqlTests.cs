using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class ContactSocialMediaSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Social_upgrade_preserves_profiles_and_enforces_channel_and_parent_integrity()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraSocialTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new CrmDbContext(options, identity);
        try
        {
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_ContactSocialMediaDetails")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
            var id = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[Records] ([Id],[TenantId],[Kind],[Name],[Status],[IsArchived],[Version],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({id},{identity.TenantId},'contact','Existing contact','active',0,{Guid.NewGuid()},{now},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [crm].[ContactProfiles] ([Id],[TenantId],[RecordId],[FirstName]) VALUES ({Guid.NewGuid()},{identity.TenantId},{id},'Existing')");
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var profile = await db.Set<ContactProfile>().SingleAsync(); Assert.Equal("Existing", profile.FirstName); Assert.Null(profile.NumberOfFollowers); Assert.Empty(await db.Set<ContactSocialProfile>().ToListAsync());
            profile.NumberOfFollowers = 3000000000L; profile.NumberOfFollowing = 0; profile.NumberOfTweetsSent = 12; profile.NumberOfMentions = 13; profile.NumberOfTweetsWithTags = 14;
            profile.SocialMetricsObservedOn = new DateOnly(2001, 2, 3); profile.SocialMetricsSource = "Manual review";
            foreach (var channel in new[] { "facebook", "x", "linkedin", "youtube", "online-communities", "google" }) db.Add(new ContactSocialProfile { TenantId = identity.TenantId!.Value, RecordId = id, Channel = channel, Identifier = channel + "-user" });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear(); profile = await db.Set<ContactProfile>().SingleAsync();
            Assert.Equal(3000000000L, profile.NumberOfFollowers); Assert.Equal(0, profile.NumberOfFollowing); Assert.Equal(new DateOnly(2001, 2, 3), profile.SocialMetricsObservedOn); Assert.Equal("Manual review", profile.SocialMetricsSource);
            Assert.Equal(6, await db.Set<ContactSocialProfile>().CountAsync());
            db.Add(new ContactSocialProfile { TenantId = identity.TenantId!.Value, RecordId = id, Channel = "facebook", Identifier = "Duplicate" }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            var account = new CrmRecord { TenantId = identity.TenantId.Value, Kind = "account", Name = "Account" }; var other = new CrmRecord { TenantId = identity.TenantId.Value, Name = "Other contact" }; db.AddRange(account, other); await db.SaveChangesAsync();
            var linked = await db.Set<ContactSocialProfile>().FirstAsync(); linked.RecordId = other.Id; await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            db.Add(new ContactSocialProfile { TenantId = identity.TenantId.Value, RecordId = account.Id, Channel = "x", Identifier = "Wrong kind" }); await Assert.ThrowsAsync<CrmBoundaryException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
            await using var outside = new CrmDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty(await outside.Set<ContactSocialProfile>().ToListAsync()); Assert.Empty(await outside.Set<ContactProfile>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
