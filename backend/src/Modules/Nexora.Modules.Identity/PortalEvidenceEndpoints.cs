using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexora.BuildingBlocks.Documents;
using Nexora.BuildingBlocks.Security;

namespace Nexora.Modules.Identity;

internal static partial class PortalEndpoints
{
    private static void MapEvidence(RouteGroupBuilder group)
    {
        static Guid Contact(HttpContext h) => (Guid)h.Items["portal_contact"]!;
        static Guid? Account(HttpContext h) => Guid.TryParse(h.Request.RouteValues["accountId"]?.ToString(), out var id) ? id : null;
        group.MapGet("/drafts/{draftId:guid}/evidence", async (Guid draftId, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
            Results.Ok(await s.DraftListAsync(Contact(h), draftId, Account(h), ct)));
        group.MapPost("/drafts/{draftId:guid}/evidence", async (Guid draftId, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
        {
            // Verify ownership before reading any uploaded bytes.
            await s.DraftListAsync(Contact(h), draftId, Account(h), ct);
            var upload = await DocumentSafety.ReadAsync(h.Request, ct);
            var form = await h.Request.ReadFormAsync(ct);
            if (form["version"].Count != 1 || !Guid.TryParse(form["version"], out var version)) return Results.BadRequest();
            return Results.Ok(await s.UploadAsync(Contact(h), draftId, Account(h), version, upload, ct));
        });
        group.MapGet("/drafts/{draftId:guid}/evidence/{fileId:guid}", async (Guid draftId, Guid fileId, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
        {
            var file = await s.DownloadDraftAsync(Contact(h), draftId, Account(h), fileId, ct);
            return DocumentSafety.Download(h, file.Name, file.Content);
        });
        group.MapDelete("/drafts/{draftId:guid}/evidence/{fileId:guid}", async (Guid draftId, Guid fileId, [Microsoft.AspNetCore.Mvc.FromBody] JsonElement body, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
        {
            if (!Fields(body, "version") || !GuidValue(body, "version", out var version)) return Results.BadRequest();
            return Results.Ok(await s.RemoveAsync(Contact(h), draftId, Account(h), fileId, version, ct));
        });
        group.MapPost("/drafts/{draftId:guid}/evidence/{fileId:guid}/scan", async (Guid draftId, Guid fileId, JsonElement body, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
        {
            if (!Fields(body, "version") || !GuidValue(body, "version", out var version)) return Results.BadRequest();
            return Results.Ok(await s.ScanDraftAsync(Contact(h), draftId, Account(h), fileId, version, ct));
        });
        group.MapGet("/applications/{id:guid}/evidence", async (Guid id, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
            Results.Ok(await s.ApplicationListAsync(id, Contact(h), Account(h), ct)));
        group.MapGet("/applications/{id:guid}/evidence/{fileId:guid}", async (Guid id, Guid fileId, HttpContext h, IApplicationEvidenceService s, CancellationToken ct) =>
        {
            var file = await s.DownloadApplicationAsync(id, Contact(h), Account(h), fileId, ct);
            return DocumentSafety.Download(h, file.Name, file.Content);
        });
    }
}
