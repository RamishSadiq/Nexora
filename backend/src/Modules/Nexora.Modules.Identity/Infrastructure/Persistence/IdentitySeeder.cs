using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Security;

namespace Nexora.Modules.Identity.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedDevelopmentIdentityAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var password = configuration["Identity:SeedAdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
        using var systemAccess = dbContext.BeginSystemAccess();
        await dbContext.Database.MigrateAsync(cancellationToken);

        const string tenantSlug = "nexora-demo";
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(x => x.Slug == tenantSlug, cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant { Name = "Nexora Demo", Slug = tenantSlug };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<NexoraRole>>();
        var role = await roleManager.Roles.SingleOrDefaultAsync(
            x => x.TenantId == tenant.Id && x.NormalizedName == "PLATFORM ADMINISTRATOR",
            cancellationToken);
        if (role is null)
        {
            role = new NexoraRole
            {
                TenantId = tenant.Id,
                Name = "Platform Administrator",
                Description = "Full tenant administration access",
                IsSystem = true,
            };
            var roleResult = await roleManager.CreateAsync(role);
            EnsureSucceeded(roleResult);
        }

        foreach (var permission in NexoraPermissions.All)
        {
            if (!await dbContext.RolePermissions.AnyAsync(
                    x => x.RoleId == role.Id && x.Permission == permission,
                    cancellationToken))
            {
                dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = permission });
            }
        }

        var email = configuration["Identity:SeedAdminEmail"] ?? "admin@nexora.local";
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<NexoraUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new NexoraUser
            {
                TenantId = tenant.Id,
                DisplayName = "Nexora Administrator",
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };
            var userResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(userResult);
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role.Name!));
        }
        else if (configuration.GetValue<bool>("Identity:ResetSeedAdminPassword"))
        {
            EnsureSucceeded(await userManager.RemovePasswordAsync(user));
            EnsureSucceeded(await userManager.AddPasswordAsync(user, password));
        }

        if (!await dbContext.Teams.AnyAsync(x => x.TenantId == tenant.Id, cancellationToken))
        {
            dbContext.Teams.Add(new Team
            {
                TenantId = tenant.Id,
                Name = "Customer Success",
                Description = "Owns customer relationships and retention",
                Members = [new TeamMember { UserId = user.Id }],
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
