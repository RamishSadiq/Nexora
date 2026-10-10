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
    public sealed record ClassificationRequest(Guid Version, Guid? DefinitionId, DateOnly EffectiveOn, string Reason);

    private static void MapMembershipClassifications(RouteGroupBuilder api)
    {
        api.MapGet("/members/{id:guid}/classifications", async (Guid id, string? page, MembershipDbContext db, CancellationToken ct) =>
        {
            var member = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            var paging = Pagination(page, "25");
            var query = db.Set<MembershipClassificationChange>().AsNoTracking().Where(x => x.MembershipId == id);
            var current = MembershipClassificationFields.Read(id, await MembershipClassificationFields.CurrentChangesAsync(db, [id], ct));
            return Results.Ok(new { member.Version, current, total = await query.CountAsync(ct), page = paging.Number, pageSize = paging.Size,
                items = await query.OrderByDescending(x => x.EffectiveOn).ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Sequence).ThenBy(x => x.Id).Skip(paging.Offset).Take(paging.Size)
                    .Select(x => new { x.Id, x.Kind, x.DefinitionId, x.NameSnapshot, x.EffectiveOn, x.Sequence, x.Reason, x.ActorUserId, x.CreatedAtUtc }).ToListAsync(ct) });
        });
        api.MapPost("/members/{id:guid}/classifications/{kind}", async (Guid id, string kind, ClassificationRequest request, MembershipDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var setupKind = kind switch { "member-type" => "member-type", "category" => "member-category", "subscription-band" => "member-subscription-band", _ => throw new ValidationException("Choose member-type, category or subscription-band.") };
            var member = await db.Set<MemberSubscription>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (member.Version != request.Version) throw new DbUpdateConcurrencyException();
            if (request.EffectiveOn == DateOnly.MinValue || request.EffectiveOn > DateOnly.FromDateTime(DateTime.UtcNow)) throw new ValidationException("Provide an effective date on or before today.");
            var reason = AdministrativeText(request.Reason, 500, false) ?? throw new ValidationException("Provide a reason for this classification change.");
            var latest = await db.Set<MembershipClassificationChange>().Where(x => x.MembershipId == id && x.Kind == kind).OrderByDescending(x => x.Sequence).FirstOrDefaultAsync(ct);
            if (latest != null && request.EffectiveOn < latest.EffectiveOn) throw new ValidationException("The effective date cannot precede the latest recorded change for this classification.");
            MembershipSetupValue? definition = null;
            if (request.DefinitionId.HasValue) definition = await db.Set<MembershipSetupValue>().SingleOrDefaultAsync(x => x.Id == request.DefinitionId && x.Kind == setupKind, ct) ?? throw new ValidationException("The selected classification is unavailable or belongs to a different lookup list.");
            if (latest?.DefinitionId == request.DefinitionId) throw new ValidationException("Choose a different classification; there is no change to record.");
            db.Add(new MembershipClassificationChange { TenantId = member.TenantId, MembershipId = id, Kind = kind, DefinitionId = definition?.Id, NameSnapshot = definition?.Name,
                EffectiveOn = request.EffectiveOn, Sequence = checked((latest?.Sequence ?? 0) + 1), ActorUserId = who.UserId!.Value, Reason = reason });
            member.Version = Guid.NewGuid(); member.UpdatedAtUtc = DateTime.UtcNow;
            History(db, who, http, id, "membership.classification.updated", "Updated " + kind);
            await db.SaveChangesAsync(ct); return Results.Ok(new { member.Version });
        }).RequireAuthorization("membership.manage");
    }
}
