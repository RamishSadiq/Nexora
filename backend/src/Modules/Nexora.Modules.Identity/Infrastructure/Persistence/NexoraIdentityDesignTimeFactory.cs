using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nexora.Modules.Identity.Infrastructure.Persistence;

public sealed class NexoraIdentityDesignTimeFactory : IDesignTimeDbContextFactory<NexoraIdentityDbContext>
{
    public NexoraIdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NEXORA_CONNECTION_STRING")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=NexoraDev;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<NexoraIdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new NexoraIdentityDbContext(options);
    }
}
