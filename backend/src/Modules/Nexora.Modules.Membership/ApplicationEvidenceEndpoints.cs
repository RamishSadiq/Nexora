using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexora.BuildingBlocks.Documents;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Membership;

internal static class ApplicationEvidenceEndpoints
{
    internal static void MapApplicationEvidence(this RouteGroupBuilder api)
    {
        var files = api.MapGroup("/applications/{id:guid}/evidence").RequireAuthorization("crm.files");
        files.AddEndpointFilter(async (context, next) => { context.HttpContext.Response.Headers.CacheControl = "no-store"; return await next(context); });
        files.MapGet("", async (Guid id, IApplicationEvidenceService s, CancellationToken ct) => Results.Ok(await s.ApplicationListAsync(id, null, null, ct)));
        files.MapGet("/{fileId:guid}", async (Guid id, Guid fileId, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
        {
            var file = await s.DownloadApplicationAsync(id, null, null, fileId, ct);
            return DocumentSafety.Download(h, file.Name, file.Content);
        });
        files.MapPost("/{fileId:guid}/scan", async (Guid id, Guid fileId, IApplicationEvidenceService s, CancellationToken ct) =>
            Results.Ok(await s.ScanApplicationAsync(id, fileId, ct))).RequireAuthorization("membership.manage");
    }
}
