using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Modules.Membership;

public sealed record ApplicationRequestedDetails(Guid? MemberTypeId = null, Guid? CategoryId = null, Guid? SubscriptionBandId = null,
    string? PaymentMethod = null, string? PaymentFrequency = null, bool? InvoiceToAccount = null, Guid? InvoicingAccountId = null,
    string? Remarks = null, string? Description = null);

public static partial class MembershipModule
{
    public sealed record ApplicationDetailsRequest(Guid Version, ApplicationRequestedDetails? Details, string Reason);
    private static void MapMembershipApplicationDetails(RouteGroupBuilder api)
    {
        api.MapGet("/applications/{id:guid}/details", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var application = await db.Set<MembershipApplication>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            return Results.Ok(ApplicationDetailsFields.Read(application));
        });
        api.MapPut("/applications/{id:guid}/details", async (Guid id, ApplicationDetailsRequest request, MembershipDbContext db, ICrmDirectory crm, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var application = await db.Set<MembershipApplication>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (application.Version != request.Version) throw new DbUpdateConcurrencyException();
            if (application.Status != "submitted") throw new ValidationException("Only submitted applications can change requested details.");
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide a reason for changing requested application details.");
            await ApplicationDetailsFields.ApplyAsync(application, request.Details ?? throw new ValidationException("Provide requested application details."), db, crm, ct);
            application.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "application.details.updated", reason);
            await db.SaveChangesAsync(ct); return Results.Ok(ApplicationDetailsFields.Read(application));
        }).RequireAuthorization("membership.manage");
    }
}
