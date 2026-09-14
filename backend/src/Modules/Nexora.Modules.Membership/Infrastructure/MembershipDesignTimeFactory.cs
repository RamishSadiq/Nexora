using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Membership.Infrastructure;
public sealed class MembershipDesignTimeFactory : IDesignTimeDbContextFactory<MembershipDbContext>
{
    private sealed class EmptyIdentity : IRequestIdentity { public Guid? TenantId => null; public Guid? UserId => null; }
    public MembershipDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MembershipDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING") ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "membership")).Options, new EmptyIdentity());
}
