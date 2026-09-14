using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Crm.Infrastructure;
public sealed class CrmDesignTimeFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    private sealed class EmptyIdentity : IRequestIdentity { public Guid? TenantId => null; public Guid? UserId => null; }
    public CrmDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<CrmDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING") ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")).Options, new EmptyIdentity());
}
