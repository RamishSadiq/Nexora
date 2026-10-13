using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Content;

internal sealed class PortalRegistrationDirectory(ContentDbContext db, IPublicTenantDirectory tenants,
    IConfiguration configuration, IHostEnvironment environment, IHttpContextAccessor accessor) : IPortalRegistrationDirectory
{
    public async Task<PortalRegistrationSite?> ResolveAsync(CancellationToken ct)
    {
        var http = accessor.HttpContext;
        if (http == null) return null;
        var site = await ContentModule.ResolvePortalSite(db, tenants, configuration, environment, http, ct);
        if (site == null) return null;
        var json = await db.Set<PortalConfiguration>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.TenantId == site.TenantId).Select(x => x.PublishedJson).SingleOrDefaultAsync(ct);
        var config = json == null ? null : JsonSerializer.Deserialize<PortalPresentation>(json);
        return new(site.TenantId, config?.Onboarding?.AllowSelfRegistration == true);
    }
}
