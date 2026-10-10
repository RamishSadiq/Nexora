using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Membership.Domain;
using Nexora.Modules.Membership.Infrastructure;
using static Nexora.BuildingBlocks.Endpoints.ModuleEndpoints;

namespace Nexora.Modules.Membership;

internal static class MembershipRejoin
{
    internal static async Task<MemberSubscription?> ValidateAsync(Guid? previousId, Guid contactId, Guid? accountId, MembershipDbContext db, CancellationToken ct)
    {
        if (previousId == null) return null;
        var previous = await db.Set<MemberSubscription>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == previousId, ct) ?? throw new KeyNotFoundException();
        Check(!previous.IsArchived,"Restore the previous membership before submitting or approving a rejoin.");
        Check(previous.AccountId == accountId && (accountId.HasValue || previous.ContactId == contactId), "Rejoin must use a previous membership for the same Contact or organisation.");
        Check(previous.Status is "cancelled" or "lapsed", "End the previous membership before submitting or approving a rejoin.");
        Check(!await db.Set<MemberSubscription>().AnyAsync(x => x.PreviousMembershipId == previous.Id, ct), "This membership already has a rejoin successor. Select the latest inactive membership instead.");
        return previous;
    }

    internal static void MapRejoinCandidates(this RouteGroupBuilder api)
    {
        api.MapGet("/rejoin-candidates", async (Guid contactId, Guid? accountId, string? page, MembershipDbContext db, CancellationToken ct) =>
        {
            Check(contactId != Guid.Empty, "Choose a Contact.");
            var number = 1; Check(page == null || int.TryParse(page, out number), "Page must be a whole number."); Check(number is >= 1 and <= 10000, "Choose a valid page.");
            var rows = db.Set<MemberSubscription>().AsNoTracking().Where(x => !x.IsArchived && x.AccountId == accountId && (accountId != null || x.ContactId == contactId) && (x.Status == "cancelled" || x.Status == "lapsed") && !db.Set<MemberSubscription>().Any(y => y.PreviousMembershipId == x.Id));
            var total = await rows.CountAsync(ct);
            var items = await (from member in rows join product in db.Set<MembershipProduct>() on member.ProductId equals product.Id
                orderby member.CreatedAtUtc descending, member.Id
                select new { member.Id, member.Status, product.Name, member.StartsAtUtc, member.EndsAtUtc })
                .Skip((number - 1) * 25).Take(25).ToListAsync(ct);
            return Results.Ok(new { items, total, page = number, pageSize = 25 });
        });
    }
}
