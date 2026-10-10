using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

public static partial class CrmModule
{
    private static void MapCommunications(RouteGroupBuilder api)
    {
        api.MapPost("/records/{id:guid}/communications", async (Guid id, CommunicationRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            if (request.Version.HasValue && parent.Version != request.Version.Value) throw new DbUpdateConcurrencyException();
            var row = new CommunicationMethod();
            ApplyCommunication(row, request.Kind, request.Value, request.Consent, request.ConsentEvidence, request.Priority);
            await ValidateCommunicationPriorityAsync(db, id, row, ct);
            return await AddChildAsync(id, row, "communication.added", db, who, http, ct);
        }).RequireAuthorization("crm.manage");

        api.MapPut("/records/{id:guid}/communications/{communicationId:guid}", async (Guid id, Guid communicationId, CommunicationUpdateRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            if (parent.Version != request.Version) throw new DbUpdateConcurrencyException();
            var row = await db.Set<CommunicationMethod>().SingleOrDefaultAsync(x => x.RecordId == id && x.Id == communicationId, ct) ?? throw new KeyNotFoundException();
            ApplyCommunication(row, request.Kind, request.Value, request.Consent, request.ConsentEvidence, request.Priority);
            await ValidateCommunicationPriorityAsync(db, id, row, ct);
            parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "communication.updated");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, parent.Version });
        }).RequireAuthorization("crm.manage");
    }

    private static void ApplyCommunication(CommunicationMethod row, string kind, string value, string consent, string? consentEvidence, string priority)
    {
        Choice(kind, "email", "phone", "mobile", "fax", "website");
        Choice(consent, "unknown", "granted", "denied"); Choice(priority, "primary", "secondary", "other");
        value = Required(value, 320);
        if (kind == "email" && !new EmailAddressAttribute().IsValid(value)) throw new ValidationException("Enter a valid email address.");
        if (kind == "website" && (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))) throw new ValidationException("Enter an HTTP or HTTPS website.");
        var evidence = consent == "unknown" ? Optional(consentEvidence, 500) : Required(consentEvidence, 500);
        row.Kind = kind; row.Value = value; row.Consent = consent; row.ConsentEvidence = evidence; row.Priority = priority;
    }

    private static async Task ValidateCommunicationPriorityAsync(CrmDbContext db, Guid recordId, CommunicationMethod row, CancellationToken ct)
    {
        if (row.Priority != "other" && await db.Set<CommunicationMethod>().AnyAsync(x => x.RecordId == recordId && x.Id != row.Id && x.Kind == row.Kind && x.Priority == row.Priority, ct))
            throw new ValidationException($"A {row.Priority} {row.Kind} already exists. Change its designation to Other before assigning another.");
    }
}
