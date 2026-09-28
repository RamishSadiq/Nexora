using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Api.Tests;

public sealed class PortalMembershipConcurrencyTests
{
    private sealed record Identity(Guid? TenantId, Guid? UserId) : IRequestIdentity;

    [SqlServerFact]
    public async Task Concurrent_activation_across_offerings_allows_one_membership_per_person_or_company()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("NEXORA_TEST_SQLSERVER"))
        { InitialCatalog = "NexoraPortalRace_" + Guid.NewGuid().ToString("N") };
        var who = new Identity(Guid.NewGuid(), Guid.NewGuid());
        var options = new DbContextOptionsBuilder<MembershipDbContext>().UseSqlServer(connection.ConnectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options;
        await using var setup = new MembershipDbContext(options, who);
        try
        {
            await setup.Database.MigrateAsync();
            foreach (var company in new[] { false, true })
            {
                var subject = Guid.NewGuid();
                var products = Enumerable.Range(0, 2).Select(i => new MembershipProduct { TenantId = who.TenantId!.Value, Name = "Race " + i }).ToArray();
                var applications = products.Select(p => new MembershipApplication { TenantId = who.TenantId!.Value,
                    ProductId = p.Id, ContactId = company ? Guid.NewGuid() : subject, AccountId = company ? subject : null,
                    ApplicationType = company ? "corporate" : "individual" }).ToArray();
                setup.AddRange(products); setup.AddRange(applications); await setup.SaveChangesAsync();
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                async Task<bool> Activate(MembershipApplication application)
                {
                    await using var db = new MembershipDbContext(options, who);
                    db.Add(new MemberSubscription { TenantId = who.TenantId!.Value, ProductId = application.ProductId,
                        ApplicationId = application.Id, ContactId = application.ContactId, AccountId = application.AccountId,
                        StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddYears(1) });
                    await gate.Task;
                    try { await db.SaveChangesAsync(); return true; }
                    catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 }) { return false; }
                }
                var attempts = applications.Select(Activate).ToArray(); gate.SetResult();
                var results = await Task.WhenAll(attempts);
                Assert.Single(results, x => x);
                Assert.Equal(1, await setup.Set<MemberSubscription>().CountAsync(x => company ? x.AccountId == subject : x.AccountId == null && x.ContactId == subject));
            }
        }
        finally { await setup.Database.EnsureDeletedAsync(); }
    }
}
