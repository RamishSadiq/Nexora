using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class MembershipApplicationDetailsSqlTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Application_details_upgrade_preserves_legacy_values_and_enforces_tenant_lookup_links()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER")) { InitialCatalog = "NexoraApplicationDetailsTest_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options;
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var db = new MembershipDbContext(options, identity);
        try
        {
            await db.Database.MigrateAsync();
            var product = new MembershipProduct { TenantId = identity.TenantId!.Value, Name = "Application SQL", Category = "Standard", Rate = 125 };
            var application = new MembershipApplication { TenantId = identity.TenantId.Value, ProductId = product.Id, ContactId = Guid.NewGuid(), QuotedRate = 125, ApplicantNotes = "Retained applicant notes", RequestedStartUtc = new DateTime(2026, 1, 2), RequestedEndUtc = new DateTime(2027, 1, 2), TermMonths = 12 };
            db.AddRange(product, application); await db.SaveChangesAsync(); var version = application.Version; db.ChangeTracker.Clear();
            var migrations = db.Database.GetMigrations().ToArray(); var index = Array.FindIndex(migrations, x => x.EndsWith("_MembershipApplicationRequestedDetails")); Assert.True(index > 0);
            await db.GetService<IMigrator>().MigrateAsync(migrations[index - 1]); await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            application = await db.Set<MembershipApplication>().SingleAsync(); Assert.Equal(version, application.Version); Assert.Equal(125, application.QuotedRate); Assert.Equal("Retained applicant notes", application.ApplicantNotes); Assert.Equal(new DateTime(2027, 1, 2), application.RequestedEndUtc);
            Assert.Null(application.LifecyclePurpose); Assert.Null(application.StatusEffectiveOn); Assert.Null(application.RequestedMemberTypeId); Assert.Null(application.InvoiceToAccount); Assert.Null(application.StaffRemarks);
            var definitions = new[] { "member-type", "member-category", "member-subscription-band" }.Select(kind => new MembershipSetupValue { TenantId = identity.TenantId.Value, Kind = kind, Name = kind }).ToArray(); db.AddRange(definitions); await db.SaveChangesAsync();
            application.RequestedMemberTypeId = definitions[0].Id; application.RequestedMemberTypeName = "Recorded type";
            application.RequestedCategoryId = definitions[1].Id; application.RequestedCategoryName = "Recorded category";
            application.RequestedSubscriptionBandId = definitions[2].Id; application.RequestedSubscriptionBandName = "Recorded band";
            application.LifecyclePurpose = "new"; application.StatusEffectiveOn = new DateOnly(2026, 10, 4); application.PaymentMethod = "Bank transfer"; application.PaymentFrequency = "Annual"; application.InvoiceToAccount = false;
            application.StaffRemarks = new string('r', 4000); application.Description = new string('d', 4000); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            application = await db.Set<MembershipApplication>().SingleAsync(); Assert.Equal(4000, application.StaffRemarks!.Length); Assert.Equal(4000, application.Description!.Length); Assert.Equal("Recorded type", application.RequestedMemberTypeName); Assert.Equal(false, application.InvoiceToAccount); Assert.Equal(new DateOnly(2026, 10, 4), application.StatusEffectiveOn);
            var outsideIdentity = new Identity(Guid.NewGuid(), Guid.NewGuid()); await using var outside = new MembershipDbContext(options, outsideIdentity); Assert.Empty(await outside.Set<MembershipApplication>().ToListAsync());
            var outsideDefinition = new MembershipSetupValue { TenantId = outsideIdentity.TenantId!.Value, Kind = "member-type", Name = "Outside" }; outside.Add(outsideDefinition); await outside.SaveChangesAsync();
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [membership].[Applications] SET [RequestedMemberTypeId] = {outsideDefinition.Id} WHERE [Id] = {application.Id}"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [membership].[Applications] SET [RequestedCategoryId] = {outsideDefinition.Id} WHERE [Id] = {application.Id}"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [membership].[Applications] SET [RequestedSubscriptionBandId] = {outsideDefinition.Id} WHERE [Id] = {application.Id}"));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [membership].[SetupValues] WHERE [Id] = {definitions[0].Id}"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
