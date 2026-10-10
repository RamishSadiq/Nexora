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
    public sealed record MembershipArchiveRequest(Guid Version, bool? IsArchived, string Reason);
    private static void MapMembershipArchive(RouteGroupBuilder api)
    {
        api.MapPut("/members/{id:guid}/archive", async (Guid id, MembershipArchiveRequest request, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (member.Version != request.Version) throw new DbUpdateConcurrencyException();
            var archived = request.IsArchived ?? throw new ValidationException("Specify whether to archive or restore the membership.");
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide an archive or restore reason.");
            if (member.IsArchived == archived) throw new ValidationException("This membership already has the requested archive state.");
            if (archived && member.Status is not ("cancelled" or "lapsed")) throw new ValidationException("End the membership before archiving it.");
            member.IsArchived = archived;
            member.ArchivedAtUtc = archived ? DateTime.UtcNow : null;
            member.ArchivedByUserId = archived ? who.UserId : null;
            member.ArchiveReason = archived ? reason : null;
            History(db, who, http, id, archived ? "membership.archived" : "membership.restored", reason);
            await db.SaveChangesAsync(ct);
            return Results.Ok(member);
        }).RequireAuthorization("membership.manage");
    }
}
