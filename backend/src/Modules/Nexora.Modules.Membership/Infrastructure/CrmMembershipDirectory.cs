using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Membership.Domain;

namespace Nexora.Modules.Membership.Infrastructure;

public sealed class CrmMembershipDirectory(MembershipDbContext db) : ICrmMembershipDirectory
{
    public async Task<CrmMembershipPage> ReadAsync(Guid recordId, string kind, int page, int pageSize, CancellationToken ct)
    {
        var members = db.Set<MemberSubscription>().AsNoTracking().Where(x => kind == "account" ? x.AccountId == recordId : x.ContactId == recordId);
        var total = await members.CountAsync(ct);
        var rows = await (from member in members join product in db.Set<MembershipProduct>().AsNoTracking() on member.ProductId equals product.Id
            orderby member.Status == "active" descending, member.CreatedAtUtc descending, member.Id
            select new { Member = member, ProductName = product.Name, ProductCategory = product.Category })
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var memberIds = rows.Select(x => x.Member.Id).ToArray(); var applicationIds = rows.Select(x => x.Member.ApplicationId).ToArray();
        var classifications = await MembershipClassificationFields.CurrentChangesAsync(db, memberIds, ct);
        var applications = await db.Set<MembershipApplication>().AsNoTracking().Where(x => applicationIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var reviews = await (from review in db.Set<MembershipReview>().AsNoTracking() join member in db.Set<MemberSubscription>().AsNoTracking() on review.MembershipId equals member.Id
            where memberIds.Contains(member.Id) && review.TermEndUtc == member.EndsAtUtc select review).ToListAsync(ct);
        // Project explicit lifecycle receipts. Review updates and other history are not status changes.
        var lifecycle = await db.Set<MembershipHistory>().AsNoTracking().Where(x => memberIds.Contains(x.SubjectId) &&
            (x.Action == "membership.activated" || x.Action == "membership.renew" || x.Action == "membership.cancel" || x.Action == "membership.resign" || x.Action == "membership.rejoin" || x.Action == "membership.lapse" || x.Action == "membership.reinstate"))
            .GroupBy(x => x.SubjectId).Select(group => group.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).First()).ToListAsync(ct);
        var items = rows.Select(row =>
        {
            var member = row.Member; applications.TryGetValue(member.ApplicationId, out var application);
            if (application?.ContactId != member.ContactId || application?.AccountId != member.AccountId) application = null;
            var review = reviews.SingleOrDefault(x => x.MembershipId == member.Id && x.TermEndUtc == member.EndsAtUtc);
            var latest = lifecycle.SingleOrDefault(x => x.SubjectId == member.Id);
            return new CrmMembershipSummary(member.Id, member.AccountId == null ? "personal" : "organisation", member.AccountId, member.AccountName,
                row.ProductName, row.ProductCategory, member.Status, member.StartsAtUtc, member.EndsAtUtc, member.Rate, member.Currency,
                member.ApplicationId, application?.CreatedAtUtc, application?.ApplicationType, review?.ExcludedFromRenewal, review?.Notes,
                latest?.Action, latest?.CreatedAtUtc, latest?.Reason,
                new MembershipAdministrativeDetails(member.LegacyMemberReference, member.MembershipPackSent, member.MembershipPackSentDate, member.Remarks), MembershipClassificationFields.Read(member.Id, classifications), member.OriginalJoinDate, new MembershipLifecycleDates(member.StatusEffectiveOn, member.ResignedOn, member.ResignationReason, member.ReinstatedOn), latest?.EffectiveOn, member.PreviousMembershipId, member.RejoinedOn, member.IsArchived, member.ArchivedAtUtc, member.ArchiveReason, new MembershipOptionsDetails(member.ExemptJoiningFee, member.FreeMembership, member.ExcludedFromLapse, member.PrimaryMembership), MembershipFinancialArrangementFields.Read(member));
        }).ToArray();
        return new(total, page, pageSize, items);
    }
}
