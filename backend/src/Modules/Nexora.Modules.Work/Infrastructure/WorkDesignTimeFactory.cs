using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Work.Infrastructure;
public sealed class WorkDesignTimeFactory : IDesignTimeDbContextFactory<WorkDbContext>
{
    private sealed class EmptyIdentity : IRequestIdentity { public Guid? TenantId => null; public Guid? UserId => null; }
    public WorkDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<WorkDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING") ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "work")).Options, new EmptyIdentity());
}
