using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity.Security;

internal sealed class RequestIdentity(IHttpContextAccessor accessor) : IRequestIdentity
{
    private Guid? Claim(string type) => accessor.HttpContext?.User.Identity?.IsAuthenticated == true &&
        Guid.TryParse(accessor.HttpContext.User.FindFirstValue(type), out var value) ? value : null;
    public Guid? TenantId => Claim(NexoraClaimTypes.TenantId);
    public Guid? UserId => Claim(ClaimTypes.NameIdentifier);
}

internal sealed class IdentityDirectoryService(NexoraIdentityDbContext db) : IIdentityDirectory
{
    public async Task<IdentityDirectory> GetAsync(CancellationToken ct) => new(
        await db.Users.Where(x => x.IsActive).OrderBy(x => x.DisplayName).Select(x => new DirectoryItem(x.Id, x.DisplayName)).ToListAsync(ct),
        await db.Teams.OrderBy(x => x.Name).Select(x => new DirectoryItem(x.Id, x.Name)).ToListAsync(ct));

    public async Task<bool> IsValidOwnerAsync(Guid? userId, Guid? teamId, CancellationToken ct) =>
        (userId == null || await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, ct)) &&
        (teamId == null || await db.Teams.AnyAsync(x => x.Id == teamId, ct));
}
