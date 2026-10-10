using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;

namespace Nexora.Modules.Membership;

public static partial class MembershipModule
{
    public sealed record MembershipAdministrationRequest(Guid Version, MembershipAdministrativeDetails? Details);

    private static void MapMembershipAdministration(RouteGroupBuilder api)
    {
        api.MapGet("/members/{id:guid}/administration", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            return Results.Ok(new { member.Version, details = ReadAdministration(member) });
        });
        api.MapPut("/members/{id:guid}/administration", async (Guid id, MembershipAdministrationRequest request, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (member.Version != request.Version) throw new DbUpdateConcurrencyException();
            var details = request.Details ?? throw new ValidationException("Provide membership administrative details.");
            var reference = AdministrativeText(details.LegacyMemberReference, 160, false);
            var remarks = AdministrativeText(details.Remarks, 4000, true);
            if (details.MembershipPackSentDate is DateOnly sent && (details.MembershipPackSent != true || sent == DateOnly.MinValue || sent > DateOnly.FromDateTime(DateTime.UtcNow)))
                throw new ValidationException("A pack sent date requires Pack sent = Yes and a valid date on or before today.");
            member.LegacyMemberReference = reference; member.MembershipPackSent = details.MembershipPackSent; member.MembershipPackSentDate = details.MembershipPackSentDate; member.Remarks = remarks;
            // Touch even on a no-op save so concurrent administration and lifecycle writes share one version.
            member.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "membership.administration.updated", "Updated membership administrative details");
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { member.Version, details = ReadAdministration(member) });
        }).RequireAuthorization("membership.manage");
    }

    private static MembershipAdministrativeDetails ReadAdministration(MemberSubscription member) => new(member.LegacyMemberReference, member.MembershipPackSent, member.MembershipPackSentDate, member.Remarks);
    private static string? AdministrativeText(string? value, int max, bool multiline)
    {
        if (value?.Length > max || value?.Any(c => char.IsControl(c) && !(multiline && c is '\r' or '\n' or '\t')) == true)
            throw new ValidationException($"Use at most {max} characters without unsupported control characters.");
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
