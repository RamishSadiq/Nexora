using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Identity.Infrastructure.Persistence;

namespace Nexora.Modules.Identity.Security;

internal sealed class RequestIdentity(IHttpContextAccessor accessor, WorkflowExecutionContext execution) : IRequestIdentity
{
    private Guid? Claim(string type) => accessor.HttpContext?.User.Identity?.IsAuthenticated == true &&
        Guid.TryParse(accessor.HttpContext.User.FindFirstValue(type), out var value) ? value : null;
    public Guid? TenantId => Claim(NexoraClaimTypes.TenantId) ?? execution.Current?.TenantId;
    public Guid? UserId => Claim(ClaimTypes.NameIdentifier) ?? execution.Current?.UserId;
}

internal sealed class IdentityDirectoryService(NexoraIdentityDbContext db) : IIdentityDirectory
{
    public async Task<IdentityDirectory> GetAsync(CancellationToken ct) => new(
        await db.Users.Where(x => x.IsActive).OrderBy(x => x.DisplayName).Select(x => new DirectoryItem(x.Id, x.DisplayName)).ToListAsync(ct),
        await db.Teams.OrderBy(x => x.Name).Select(x => new DirectoryItem(x.Id, x.Name)).ToListAsync(ct));

    public Task<Guid[]> GetTeamMembersAsync(Guid teamId,CancellationToken ct)=>db.Teams.Where(t=>t.Id==teamId).SelectMany(t=>t.Members).Where(m=>db.Users.Any(u=>u.Id==m.UserId&&u.IsActive)).Select(m=>m.UserId).ToArrayAsync(ct);
    public async Task<bool> IsValidOwnerAsync(Guid? userId, Guid? teamId, CancellationToken ct) =>
        (userId == null || await db.Users.AnyAsync(x => x.Id == userId && x.IsActive, ct)) &&
        (teamId == null || await db.Teams.AnyAsync(x => x.Id == teamId, ct));
}

internal sealed class PublicTenantDirectory(NexoraIdentityDbContext db) : IPublicTenantDirectory
{
    public Task<bool> IsActiveAsync(Guid tenantId, CancellationToken ct) => db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == tenantId && t.Status == Nexora.Modules.Identity.Domain.TenantStatus.Active, ct);
}
