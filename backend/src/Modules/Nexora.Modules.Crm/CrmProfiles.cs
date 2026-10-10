using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Infrastructure;
using Nexora.Modules.Crm.Contracts;

namespace Nexora.Modules.Crm;

public static partial class CrmModule
{
    public sealed record ProfileRequest(Guid Version, ContactDetails? ContactDetails = null, AccountDetails? AccountDetails = null, ContactGeneralInformation? GeneralInformation = null, ContactCommunicationPermissions? CommunicationPermissions = null, ContactSocialMediaDetails? SocialMediaDetails = null, AccountGeneralInformation? AccountGeneralInformation = null);

    private static void MapProfiles(RouteGroupBuilder api)
    {
        api.MapPut("/records/{id:guid}/profile", async (Guid id, ProfileRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var record = await RecordAsync(db, id, ct, true);
            if (record.Version != request.Version) throw new DbUpdateConcurrencyException();
            if (record.Kind == "contact")
            {
                if (request.ContactDetails == null || request.AccountDetails != null || request.AccountGeneralInformation != null) throw new ValidationException("Provide contact details only.");
                await RecordProfileFields.ApplyContactAsync(db, record, request.ContactDetails, ct);
                if (request.GeneralInformation != null)
                    await RecordProfileFields.ApplyGeneralInformationAsync(db, record, request.GeneralInformation, ct);
                if (request.CommunicationPermissions != null)
                    await RecordProfileFields.ApplyCommunicationPermissionsAsync(db, record, request.CommunicationPermissions, ct);
                if (request.SocialMediaDetails != null)
                    await ContactSocialFields.ApplyAsync(db, record, request.SocialMediaDetails, ct);
            }
            else
            {
                if (request.AccountDetails == null || request.ContactDetails != null || request.GeneralInformation != null || request.CommunicationPermissions != null || request.SocialMediaDetails != null) throw new ValidationException("Provide account details only.");
                await RecordProfileFields.ApplyAccountAsync(db, record, request.AccountDetails, ct);
                if (request.AccountGeneralInformation != null) await RecordProfileFields.ApplyAccountGeneralInformationAsync(db, record, request.AccountGeneralInformation, ct);
            }
            record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "profile.updated");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { record.Version });
        }).RequireAuthorization("crm.manage");
    }
}
