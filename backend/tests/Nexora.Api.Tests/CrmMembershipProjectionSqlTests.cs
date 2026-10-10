using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class CrmMembershipProjectionSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Membership_projection_queries_sql_with_current_review_latest_lifecycle_and_tenant_isolation()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraMembershipProjectionTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new MembershipDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var contactId = Guid.NewGuid(); var accountId = Guid.NewGuid(); var start = DateTime.UtcNow.Date; var end = start.AddYears(1);
            var product = new MembershipProduct { TenantId = identity.TenantId!.Value, Name = "Annual", Category = "Current category", Rate = 999, Currency = "GBP" }; db.Add(product); await db.SaveChangesAsync();
            var personalApplication = new MembershipApplication { TenantId = identity.TenantId.Value, ProductId = product.Id, ContactId = contactId, ApplicationType = "individual", Status = "approved", RequestedStartUtc = start };
            var corporateApplication = new MembershipApplication { TenantId = identity.TenantId.Value, ProductId = product.Id, ContactId = contactId, AccountId = accountId, AccountName = "Organisation", ApplicationType = "corporate", Status = "approved", RequestedStartUtc = start };
            var legacyApplication = new MembershipApplication { TenantId = identity.TenantId.Value, ProductId = product.Id, ContactId = contactId, ApplicationType = "individual", Status = "approved", RequestedStartUtc = start.AddYears(-2) };
            db.AddRange(personalApplication, corporateApplication, legacyApplication); await db.SaveChangesAsync();
            var personal = new MemberSubscription { TenantId = identity.TenantId.Value, ProductId = product.Id, ApplicationId = personalApplication.Id, ContactId = contactId, StartsAtUtc = start, EndsAtUtc = end, Rate = 125.50m, Currency = "GBP" };
            var corporate = new MemberSubscription { TenantId = identity.TenantId.Value, ProductId = product.Id, ApplicationId = corporateApplication.Id, ContactId = contactId, AccountId = accountId, AccountName = "Organisation", StartsAtUtc = start, EndsAtUtc = end, Rate = 200, Currency = "GBP" };
            var legacy = new MemberSubscription { TenantId = identity.TenantId.Value, ProductId = product.Id, ApplicationId = legacyApplication.Id, ContactId = contactId, Status = "cancelled", StartsAtUtc = start.AddYears(-2), EndsAtUtc = start.AddYears(-1), Rate = 100, Currency = "GBP" };
            db.AddRange(personal, corporate, legacy); await db.SaveChangesAsync();
            db.AddRange(new MembershipReview { TenantId = identity.TenantId.Value, MembershipId = personal.Id, TermEndUtc = start, ExcludedFromRenewal = true, Notes = "Previous term" },
                new MembershipReview { TenantId = identity.TenantId.Value, MembershipId = personal.Id, TermEndUtc = end, ExcludedFromRenewal = false, Notes = "Current term" },
                new MembershipHistory { TenantId = identity.TenantId.Value, SubjectId = personal.Id, ActorUserId = identity.UserId!.Value, Action = "membership.activated", Reason = "Activation", CreatedAtUtc = start.AddDays(-2) },
                new MembershipHistory { TenantId = identity.TenantId.Value, SubjectId = personal.Id, ActorUserId = identity.UserId.Value, Action = "membership.renew", Reason = "Latest renewal", CreatedAtUtc = start.AddDays(-1) },
                new MembershipHistory { TenantId = identity.TenantId.Value, SubjectId = personal.Id, ActorUserId = identity.UserId.Value, Action = "review.updated", Reason = "Unrelated update", CreatedAtUtc = start });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var directory = new CrmMembershipDirectory(db); var page = await directory.ReadAsync(contactId, "contact", 1, 10, default); Assert.Equal(3, page.Total);
            var projected = Assert.Single(page.Items, x => x.Id == personal.Id); Assert.Equal(125.50m, projected.Rate); Assert.Equal("Current category", projected.ProductCategory);
            Assert.False(projected.ExcludedFromRenewal); Assert.Equal("Current term", projected.ReviewNotes); Assert.Equal("membership.renew", projected.LatestLifecycleAction); Assert.Equal("Latest renewal", projected.LatestLifecycleReason);
            var historical = Assert.Single(page.Items, x => x.Id == legacy.Id); Assert.Null(historical.LatestLifecycleAction); Assert.Null(historical.ExcludedFromRenewal);
            Assert.Equal("cancelled", page.Items.Last().Status); Assert.Equal("organisation", Assert.Single((await directory.ReadAsync(accountId, "account", 1, 10, default)).Items).Scope);
            var first = await directory.ReadAsync(contactId, "contact", 1, 1, default); var second = await directory.ReadAsync(contactId, "contact", 2, 1, default); Assert.NotEqual(first.Items[0].Id, second.Items[0].Id);
            Assert.Empty((await directory.ReadAsync(Guid.NewGuid(), "contact", 1, 10, default)).Items);
            await using var outside = new MembershipDbContext(options, new Identity(Guid.NewGuid(), Guid.NewGuid())); Assert.Empty((await new CrmMembershipDirectory(outside).ReadAsync(contactId, "contact", 1, 10, default)).Items);
            Assert.Equal(3, await db.Set<MemberSubscription>().CountAsync()); Assert.Equal(3, await db.Set<MembershipHistory>().CountAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
