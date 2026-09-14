using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Events.Infrastructure;
public sealed class EventsDesignTimeFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    private sealed class EmptyIdentity : IRequestIdentity { public Guid? TenantId => null; public Guid? UserId => null; }
    public EventsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<EventsDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING") ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "events")).Options, new EmptyIdentity());
}
