namespace Nexora.BuildingBlocks.Security;
public sealed record OwnMembership(Guid Id, string Product, string Status, DateTime StartsAtUtc, DateTime EndsAtUtc);
public sealed record OwnApplication(Guid Id, string Product, string Status, DateTime CreatedAtUtc);
public sealed record OwnMembershipOverview(IReadOnlyList<OwnMembership> Memberships, IReadOnlyList<OwnApplication> Applications);
public interface IMemberMembershipDirectory
{
    Task<OwnMembershipOverview> ReadAsync(Guid contactId, CancellationToken ct);
    Task<OwnMembershipOverview> ReadOrganisationAsync(Guid accountId, CancellationToken ct);
}
public sealed record PortalProduct(Guid Id, string Name);
public sealed record MemberDraft(Guid Id, Guid Version, Guid ProductId, DateTime StartsAtUtc, string Notes, decimal Total, string Currency, string Terms, Guid? ApplicationId);
public sealed record SaveMemberDraft(Guid Id, Guid? Version, Guid ProductId, DateTime StartsAtUtc, string Notes);
public sealed record MemberApplicationReceipt(Guid Id, string Product, string Status, DateTime SubmittedAtUtc,
    DateTime RequestedStartUtc, DateTime? RequestedEndUtc, decimal QuotedTotal, string Currency,
    DateTime? TermsAcceptedAtUtc, string? AcceptedTerms, string? SubmittedNotes, OwnMembership? Membership);
public interface IMemberApplicationService
{
    Task<IReadOnlyList<PortalProduct>> ProductsAsync(CancellationToken ct, bool organisation = false);
    Task<IReadOnlyList<MemberDraft>> DraftsAsync(Guid contactId, CancellationToken ct, Guid? accountId = null);
    Task<MemberDraft> SaveAsync(Guid contactId, SaveMemberDraft request, CancellationToken ct, Guid? accountId = null);
    Task<Guid> SubmitAsync(Guid contactId, Guid id, Guid version, bool termsAccepted, CancellationToken ct, Guid? accountId = null);
    Task DiscardAsync(Guid contactId, Guid id, Guid version, CancellationToken ct, Guid? accountId = null);
    Task<MemberApplicationReceipt> ReceiptAsync(Guid contactId, Guid id, CancellationToken ct, Guid? accountId = null);
}

