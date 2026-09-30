using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

public static partial class CrmModule
{
    public sealed record ProfileRequest(Guid Version, ContactDetails? ContactDetails = null, AccountDetails? AccountDetails = null);

    private static void MapProfiles(RouteGroupBuilder api)
    {
        api.MapPut("/records/{id:guid}/profile", async (Guid id, ProfileRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var record = await RecordAsync(db, id, ct, true);
            if (record.Version != request.Version) throw new DbUpdateConcurrencyException();
            if (record.Kind == "contact")
            {
                if (request.ContactDetails == null || request.AccountDetails != null) throw new ValidationException("Provide contact details only.");
                await RecordProfileFields.ApplyContactAsync(db, record, request.ContactDetails, ct);
            }
            else
            {
                if (request.AccountDetails == null || request.ContactDetails != null) throw new ValidationException("Provide account details only.");
                await RecordProfileFields.ApplyAccountAsync(db, record, request.AccountDetails, ct);
            }
            record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "profile.updated");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { record.Version });
        }).RequireAuthorization("crm.manage");
    }
}
