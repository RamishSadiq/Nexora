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
    public sealed record MembershipJoinDateRequest(Guid Version, DateOnly? OriginalJoinDate, string Reason);
    private static void MapMembershipJoinDate(RouteGroupBuilder api)
    {
        api.MapGet("/members/{id:guid}/join-date", async (Guid id, MembershipDbContext db, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            return Results.Ok(new { member.Version, member.OriginalJoinDate });
        });
        api.MapPut("/members/{id:guid}/join-date", async (Guid id, MembershipJoinDateRequest request, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (member.Version != request.Version) throw new DbUpdateConcurrencyException();
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide a reason for recording, correcting or clearing the original join date.");
            if (request.OriginalJoinDate is DateOnly date && (date == DateOnly.MinValue || date > DateOnly.FromDateTime(DateTime.UtcNow) || date > DateOnly.FromDateTime(member.StartsAtUtc)))
                throw new ValidationException("Original join date must be on or before today and the subscription start.");
            if (member.OriginalJoinDate == request.OriginalJoinDate) throw new ValidationException("Choose a different original join date.");
            member.OriginalJoinDate = request.OriginalJoinDate;
            member.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "membership.join-date.updated", reason);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { member.Version, member.OriginalJoinDate });
        }).RequireAuthorization("membership.manage");
    }
}
