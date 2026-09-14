using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Nexora.Modules.Identity.Security;

public static class NexoraClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string DisplayName = "display_name";
    public const string Permission = "permission";
}

public static class NexoraPermissions
{
    public const string IdentityRead = "identity.read";
    public const string IdentityManage = "identity.manage";
    public const string AuditRead = "audit.read";

    public static readonly string[] All = [IdentityRead, IdentityManage, AuditRead, "crm.read", "crm.manage", "crm.configure", "crm.files", "membership.read", "membership.manage", "membership.decide", "events.read", "events.manage", "events.results", "finance.read", "finance.manage", "finance.post", "finance.export", "engagement.read", "engagement.manage", "engagement.fundraise", "work.read", "work.manage", "work.import", "work.export", "insights.read", "insights.export"];
}

public interface ITenantContext
{
    Guid? TenantId { get; }
}

internal sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public Guid? TenantId => accessor.HttpContext?.User.Identity?.IsAuthenticated == true && Guid.TryParse(
        accessor.HttpContext?.User.FindFirstValue(NexoraClaimTypes.TenantId),
        out var tenantId)
        ? tenantId
        : null;
}
