using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Content;
namespace Nexora.Api.Tests;
public sealed class CmsUpgradeTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;
    [SqlServerFact]
    public async Task Upgrade_preserves_existing_brand_and_visible_navigation()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraCmsUpgrade_" + Guid.NewGuid().ToString("N") };
        var who = new Identity(Guid.NewGuid(), Guid.NewGuid());
        await using var db = new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "content")).Options, who);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20260918072351_InitialContent");
            var site = Guid.NewGuid(); var page = Guid.NewGuid(); var version = Guid.NewGuid(); var now = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [content].[Sites] ([Id],[TenantId],[Version],[CreatedAtUtc],[UpdatedAtUtc],[Name],[Tagline]) VALUES ({site},{who.TenantId},{version},{now},{now},'Existing association','Existing tagline')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [content].[Pages] ([Id],[TenantId],[Version],[CreatedAtUtc],[UpdatedAtUtc],[SiteId],[Slug],[DraftId]) VALUES ({page},{who.TenantId},{version},{now},{now},{site},'about',{Guid.NewGuid()})");
            await db.Database.MigrateAsync();
            Assert.Equal("#183a31", (await db.Sites.SingleAsync()).BrandColor);
            var migrated = await db.Pages.SingleAsync(); Assert.True(migrated.ShowInNavigation); Assert.Equal("About us", migrated.NavigationLabel); Assert.Equal(1, migrated.NavigationOrder);
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Empty(await db.Set<WebsiteConfiguration>().ToListAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
