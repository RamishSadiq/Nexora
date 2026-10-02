using Nexora.BuildingBlocks.Persistence;
using Nexora.Modules.Workflow;
using Nexora.Modules.Content;
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
    private readonly SqliteConnection _workflowConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _contentConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _engagementConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _workConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _financeConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _eventsConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _membershipConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _crmConnection = new("Data Source=:memory:");
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public const string AdminEmail = "admin@nexora.test";
    public const string AdminPassword = "Nexora-Test-2026!";

    private readonly Nexora.BuildingBlocks.Documents.IDocumentScanner? _scanner;
    private readonly IWorkflowTransport? _transport;
    public IdentityApiFactory() : this(null) { }
    internal IdentityApiFactory(Nexora.BuildingBlocks.Documents.IDocumentScanner? scanner,IWorkflowTransport? transport=null)
    {
        _scanner = scanner;_transport=transport;
        _connection.Open();
        _workflowConnection.Open();
        _contentConnection.Open();
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
            if(_transport!=null){services.RemoveAll<IWorkflowTransport>();services.AddSingleton(_transport);}
            if (_scanner != null) services.AddSingleton(_scanner);
            var workflowWorker=services.SingleOrDefault(d=>d.ServiceType==typeof(Microsoft.Extensions.Hosting.IHostedService)&&d.ImplementationType==typeof(WorkflowWorker));
            if(workflowWorker!=null)services.Remove(workflowWorker);
            services.RemoveAll<DbContextOptions<WorkflowDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<WorkflowDbContext>>();
            services.AddDbContext<WorkflowDbContext>(options=>options.UseSqlite(_workflowConnection));
            services.RemoveAll<DbContextOptions<ContentDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ContentDbContext>>();
            services.AddDbContext<ContentDbContext>((sp,options) => options.AddInterceptors(sp.GetRequiredService<WorkflowCaptureInterceptor>()).UseSqlite(_contentConnection));
            services.RemoveAll<DbContextOptions<EngagementDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EngagementDbContext>>();
            services.AddDbContext<EngagementDbContext>((sp,options) => options.UseSqlite(_engagementConnection).AddInterceptors(sp.GetRequiredService<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>()));
            services.RemoveAll<DbContextOptions<WorkDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<WorkDbContext>>();
            services.AddDbContext<WorkDbContext>((sp,options) => options.UseSqlite(_workConnection).AddInterceptors(sp.GetRequiredService<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>()));
            services.RemoveAll<DbContextOptions<FinanceDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FinanceDbContext>>();
            services.AddDbContext<FinanceDbContext>((sp,options) => options.UseSqlite(_financeConnection).AddInterceptors(sp.GetRequiredService<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>()));
            services.RemoveAll<DbContextOptions<EventsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EventsDbContext>>();
            services.AddDbContext<EventsDbContext>((sp,options) => options.UseSqlite(_eventsConnection).AddInterceptors(sp.GetRequiredService<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>()));
            services.RemoveAll<DbContextOptions<MembershipDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<MembershipDbContext>>();
            services.AddDbContext<MembershipDbContext>((sp,options) => options.UseSqlite(_membershipConnection).AddInterceptors(sp.GetRequiredService<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>()));
            services.RemoveAll<DbContextOptions<CrmDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CrmDbContext>>();
            services.AddDbContext<CrmDbContext>((sp,options) => options.UseSqlite(_crmConnection).AddInterceptors(sp.GetRequiredService<Nexora.BuildingBlocks.Persistence.WorkflowCaptureInterceptor>()));
            services.RemoveAll<DbContextOptions<NexoraIdentityDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<NexoraIdentityDbContext>>();
            services.AddDbContext<NexoraIdentityDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { _workflowConnection.Dispose(); _contentConnection.Dispose(); _connection.Dispose(); _crmConnection.Dispose(); _membershipConnection.Dispose(); _eventsConnection.Dispose(); _financeConnection.Dispose(); _workConnection.Dispose(); _engagementConnection.Dispose(); }
    }

    private async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<ContentDbContext>().Database.EnsureCreatedAsync();
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
