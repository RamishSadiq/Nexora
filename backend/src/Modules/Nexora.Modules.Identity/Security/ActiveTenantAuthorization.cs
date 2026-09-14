using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Identity.Domain;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity.Security;

internal sealed class ActiveTenantRequirement : IAuthorizationRequirement;

internal sealed class ActiveTenantAuthorizationHandler(NexoraIdentityDbContext dbContext)
    : AuthorizationHandler<ActiveTenantRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveTenantRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !Guid.TryParse(context.User.FindFirstValue(NexoraClaimTypes.TenantId), out var tenantId))
        {
            return;
        }

        var hasActiveBoundary = await dbContext.Users
            .Where(user => user.Id == userId && user.TenantId == tenantId && user.IsActive)
            .AnyAsync() && await dbContext.Tenants
            .Where(tenant => tenant.Id == tenantId && tenant.Status == TenantStatus.Active)
            .AnyAsync();

        if (hasActiveBoundary)
        {
            context.Succeed(requirement);
        }
    }
}
