using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;

namespace Nexora.Modules.Membership;

public static partial class MembershipModule
{
    private static readonly string[] TimelineStatusActions = ["membership.activated", "membership.renew", "membership.cancel", "membership.resign", "membership.lapse", "membership.reinstate"];
    public sealed record MembershipTimelineRow(Guid Id, Guid MembershipId, string Type, string? Value, DateOnly? EffectiveDate,
        DateOnly? CeasedDate, int? Days, string? ResignReason, string Description, DateTime RecordedAtUtc, Guid ActorUserId, string Source, int? Sequence);

    public sealed record MembershipTimelinePage(int Total, int Page, int PageSize, IReadOnlyList<MembershipTimelineRow> Items);
    private static void MapMembershipTimeline(RouteGroupBuilder api)
    {
        api.MapGet("/members/{id:guid}/timeline", async (Guid id, string? page, string? type, MembershipDbContext db, CancellationToken ct) => Results.Ok(await ReadTimelineAsync(id, page, type, db, ct)));
    }

    public static async Task<MembershipTimelinePage> ReadTimelineAsync(Guid id, string? page, string? type, MembershipDbContext db, CancellationToken ct)
    {
        _ = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        Check(type == null || type is "member-type" or "category" or "subscription-band" or "status", "Choose member-type, category, subscription-band or status.");
        var paging = Pagination(page, "25"); Check(paging.Number <= 1000, "Use a timeline page between 1 and 1000; filter by type for longer histories.");
        var classifications = db.Set<MembershipClassificationChange>().AsNoTracking().Where(x => x.MembershipId == id);
        if (type != null) classifications = classifications.Where(x => x.Kind == type);
        var statuses = db.Set<MembershipHistory>().AsNoTracking().Where(x => x.SubjectId == id && TimelineStatusActions.Contains(x.Action));
        if (type != null && type != "status") statuses = statuses.Where(x => false);
        var total = await classifications.CountAsync(ct) + await statuses.CountAsync(ct);
        if (paging.Offset >= total) return new(total, paging.Number, paging.Size, Array.Empty<MembershipTimelineRow>());
        // Load only each stream's prefix needed for this page. Every newer event is retained
        // before deriving intervals, so closure remains correct across a page boundary.
        var take = paging.Offset + paging.Size;
        var changes = await classifications.OrderByDescending(x => x.EffectiveOn).ThenBy(x => x.Kind).ThenByDescending(x => x.Sequence).ThenByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id.ToString()).Take(take).ToListAsync(ct);
        var lifecycle = await statuses.OrderByDescending(x => x.EffectiveOn).ThenByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id.ToString()).Take(take).ToListAsync(ct);
        var rows = new List<MembershipTimelineRow>();
        foreach (var group in changes.GroupBy(x => x.Kind))
        {
            DateOnly? next = null;
            foreach (var change in group.OrderByDescending(x => x.Sequence))
            {
                rows.Add(new(change.Id, id, change.Kind, change.NameSnapshot, change.EffectiveOn, next, next?.DayNumber - change.EffectiveOn.DayNumber,
                    null, change.Reason, change.CreatedAtUtc, change.ActorUserId, "classification", change.Sequence));
                next = change.EffectiveOn;
            }
        }
        DateOnly? nextStatus = null;
        foreach (var change in lifecycle)
        {
            var status = change.Action == "membership.lapse" ? "lapsed" : change.Action is "membership.cancel" or "membership.resign" ? "cancelled" : "active";
            var ceased = change.EffectiveOn.HasValue ? nextStatus : null;
            rows.Add(new(change.Id, id, "status", status, change.EffectiveOn, ceased,
                ceased.HasValue && change.EffectiveOn.HasValue ? ceased.Value.DayNumber - change.EffectiveOn.Value.DayNumber : null,
                change.Action == "membership.resign" ? change.Reason : null, change.Reason, change.CreatedAtUtc, change.ActorUserId, change.Action, null));
            if (change.EffectiveOn.HasValue) nextStatus = change.EffectiveOn;
        }
        var items = rows.OrderByDescending(x => x.EffectiveDate).ThenBy(x => x.Type, StringComparer.Ordinal).ThenByDescending(x => x.Sequence).ThenByDescending(x => x.RecordedAtUtc).ThenBy(x => x.Id.ToString(), StringComparer.Ordinal).Skip(paging.Offset).Take(paging.Size).ToArray();
        return new(total, paging.Number, paging.Size, items);
    }
}
