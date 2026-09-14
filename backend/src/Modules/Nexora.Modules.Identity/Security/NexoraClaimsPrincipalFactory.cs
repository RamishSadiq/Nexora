using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity.Security;

internal sealed class NexoraClaimsPrincipalFactory(
    UserManager<NexoraUser> userManager,
    RoleManager<NexoraRole> roleManager,
    IOptions<IdentityOptions> options,
    NexoraIdentityDbContext dbContext)
    : UserClaimsPrincipalFactory<NexoraUser, NexoraRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(NexoraUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(NexoraClaimTypes.TenantId, user.TenantId.ToString()));
        identity.AddClaim(new Claim(NexoraClaimTypes.DisplayName, user.DisplayName));

        var roleNames = await UserManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            return identity;
        }

        var permissions = await dbContext.RolePermissions
            .Where(permission =>
                permission.Role.TenantId == user.TenantId &&
                roleNames.Contains(permission.Role.Name!))
            .Select(permission => permission.Permission)
            .Distinct()
            .ToListAsync();

        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(NexoraClaimTypes.Permission, permission));
        }

        return identity;
    }
}
