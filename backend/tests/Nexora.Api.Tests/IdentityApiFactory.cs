using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Membership.Infrastructure;
using Nexora.Modules.Events.Infrastructure;
using Nexora.Modules.Finance.Infrastructure;
using Nexora.Modules.Work.Infrastructure;
using Nexora.Modules.Engagement.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;
using Nexora.Modules.Identity.Security;

namespace Nexora.Api.Tests;

public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _engagementConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _workConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _financeConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _eventsConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _membershipConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _crmConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public const string AdminEmail = "admin@nexora.test";
    public const string AdminPassword = "Nexora-Test-2026!";

    public IdentityApiFactory()
    {
        _connection.Open();
        _crmConnection.Open();
        _membershipConnection.Open();
        _eventsConnection.Open();
        _financeConnection.Open();
        _workConnection.Open();
        _engagementConnection.Open();
        SeedAsync().GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<EngagementDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EngagementDbContext>>();
            services.AddDbContext<EngagementDbContext>(options => options.UseSqlite(_engagementConnection));
            services.RemoveAll<DbContextOptions<WorkDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<WorkDbContext>>();
            services.AddDbContext<WorkDbContext>(options => options.UseSqlite(_workConnection));
            services.RemoveAll<DbContextOptions<FinanceDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FinanceDbContext>>();
            services.AddDbContext<FinanceDbContext>(options => options.UseSqlite(_financeConnection));
            services.RemoveAll<DbContextOptions<EventsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EventsDbContext>>();
            services.AddDbContext<EventsDbContext>(options => options.UseSqlite(_eventsConnection));
            services.RemoveAll<DbContextOptions<MembershipDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<MembershipDbContext>>();
            services.AddDbContext<MembershipDbContext>(options => options.UseSqlite(_membershipConnection));
            services.RemoveAll<DbContextOptions<CrmDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CrmDbContext>>();
            services.AddDbContext<CrmDbContext>(options => options.UseSqlite(_crmConnection));
            services.RemoveAll<DbContextOptions<NexoraIdentityDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<NexoraIdentityDbContext>>();
            services.AddDbContext<NexoraIdentityDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { _connection.Dispose(); _crmConnection.Dispose(); _membershipConnection.Dispose(); _eventsConnection.Dispose(); _financeConnection.Dispose(); _workConnection.Dispose(); _engagementConnection.Dispose(); }
    }

    private async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<FinanceDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<WorkDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.EnsureCreatedAsync();
        var context = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
        using var systemAccess = context.BeginSystemAccess();
        await context.Database.EnsureCreatedAsync();

        var primaryTenant = new Tenant { Name = "Northstar Group", Slug = "northstar" };
        var secondaryTenant = new Tenant { Name = "Outside Tenant", Slug = "outside" };
        context.Tenants.AddRange(primaryTenant, secondaryTenant);
        await context.SaveChangesAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<NexoraRole>>();
        var primaryRole = new NexoraRole
        {
            TenantId = primaryTenant.Id,
            Name = "Platform Administrator",
            Description = "Full tenant administration access",
            IsSystem = true,
        };
        Assert.True((await roleManager.CreateAsync(primaryRole)).Succeeded);

        context.RolePermissions.AddRange(NexoraPermissions.All.Select(permission =>
            new RolePermission { RoleId = primaryRole.Id, Permission = permission }));

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<NexoraUser>>();
        var admin = new NexoraUser
        {
            TenantId = primaryTenant.Id,
            DisplayName = "Avery Morgan",
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
        };
        Assert.True((await userManager.CreateAsync(admin, AdminPassword)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(admin, primaryRole.Name!)).Succeeded);

        var outsideUser = new NexoraUser
        {
            TenantId = secondaryTenant.Id,
            DisplayName = "Outside User",
            UserName = "outside@nexora.test",
            Email = "outside@nexora.test",
            EmailConfirmed = true,
        };
        Assert.True((await userManager.CreateAsync(outsideUser, AdminPassword)).Succeeded);

        var outsideRole = new NexoraRole { TenantId = secondaryTenant.Id, Name = "Outside Administrator" };
        Assert.True((await roleManager.CreateAsync(outsideRole)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(outsideUser, outsideRole.Name!)).Succeeded);
        context.RolePermissions.Add(new RolePermission { RoleId = outsideRole.Id, Permission = NexoraPermissions.IdentityRead });

        context.RolePermissions.Add(new RolePermission { RoleId = outsideRole.Id, Permission = "crm.read" });

        context.Teams.AddRange(
            new Team
            {
                TenantId = primaryTenant.Id,
                Name = "Customer Success",
                Members = [new TeamMember { UserId = admin.Id }],
            },
            new Team { TenantId = secondaryTenant.Id, Name = "Outside Team" });
        await context.SaveChangesAsync();
    }
}
