namespace Nexora.BuildingBlocks.Security;

public sealed record MembershipAdministrativeDetails(string? LegacyMemberReference = null, bool? MembershipPackSent = null, DateOnly? MembershipPackSentDate = null, string? Remarks = null);
public sealed record MembershipFinancialArrangementDetails(int? RenewalMonth = null, DateOnly? RenewalDate = null, DateOnly? NextRenewalDate = null, string? PromotionCode = null, string? PaymentMethod = null, string? PaymentFrequency = null, Guid? PaidById = null, string? PaidByName = null, string? PaidByKind = null, bool? InvoiceToAccount = null, Guid? InvoicingAccountId = null, string? InvoicingAccountName = null, string? PaymentProvider = null);
public sealed record MembershipOptionsDetails(bool? ExemptJoiningFee = null, bool? FreeMembership = null, bool? ExcludedFromLapse = null, bool? PrimaryMembership = null);
public sealed record MembershipClassification(Guid? DefinitionId, string? Name, DateOnly? EffectiveOn);
public sealed record MembershipClassifications(MembershipClassification Category, MembershipClassification SubscriptionBand, MembershipClassification MemberType);

public sealed record MembershipLifecycleDates(DateOnly? StatusDate, DateOnly? ResignDate, string? ResignReason, DateOnly? ReinstatementDate);

public sealed record CrmMembershipSummary(
    Guid Id, string Scope, Guid? AccountId, string? AccountName, string ProductName, string ProductCategory,
    string Status, DateTime StartsAtUtc, DateTime EndsAtUtc, decimal Rate, string Currency,
    Guid ApplicationId, DateTime? ApplicationDateUtc, string? ApplicationType,
    bool? ExcludedFromRenewal, string? ReviewNotes,
    string? LatestLifecycleAction, DateTime? LatestLifecycleAtUtc, string? LatestLifecycleReason,
    MembershipAdministrativeDetails Administration, MembershipClassifications Classifications, DateOnly? OriginalJoinDate = null, MembershipLifecycleDates? LifecycleDates = null, DateOnly? LatestLifecycleEffectiveOn = null, Guid? PreviousMembershipId = null, DateOnly? RejoinedOn = null, bool IsArchived = false, DateTime? ArchivedAtUtc = null, string? ArchiveReason = null, MembershipOptionsDetails? Options = null, MembershipFinancialArrangementDetails? FinancialArrangement = null);
public sealed record CrmMembershipPage(int Total, int Page, int PageSize, IReadOnlyList<CrmMembershipSummary> Items);

public interface ICrmMembershipDirectory
{
    Task<CrmMembershipPage> ReadAsync(Guid recordId, string kind, int page, int pageSize, CancellationToken ct);
}
