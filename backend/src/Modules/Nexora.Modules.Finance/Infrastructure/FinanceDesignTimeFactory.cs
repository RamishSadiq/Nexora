using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Finance.Infrastructure;
public sealed class FinanceDesignTimeFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    private sealed class EmptyIdentity : IRequestIdentity { public Guid? TenantId => null; public Guid? UserId => null; }
    public FinanceDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<FinanceDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING") ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "finance")).Options, new EmptyIdentity());
}
