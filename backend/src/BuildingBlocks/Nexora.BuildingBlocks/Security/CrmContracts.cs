namespace Nexora.BuildingBlocks.Security;
public sealed record CrmIdentity(Guid Id, string Name, string Kind);
public interface ICrmDirectory
{
    Task<string?> MarketingEmailAsync(Guid contactId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CrmIdentity>> SearchContactsAsync(string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<CrmIdentity>> SearchAccountsAsync(string? search, CancellationToken cancellationToken);
    Task<bool> HasCurrentAccountContactAsync(Guid accountId, Guid contactId, CancellationToken cancellationToken);
    Task<CrmIdentity?> FindActiveAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record MemberPostalAddress(string Line1, string? Line2, string City, string? Region, string PostalCode, string Country);
public sealed record ContactDetails(string? Title = null, string? FirstName = null, string? MiddleName = null, string? LastName = null, string? PreviousLastName = null, string? PostNominal = null);
public sealed record AccountDetails(string? FullName = null, string? TradingAs = null, DateOnly? DateBusinessCommenced = null);
public sealed record MemberOrganisationAssociation(string? Department, string? JobTitle, bool IsMainAccount);
public sealed record OrganisationProfile(string Name, AccountDetails? AccountDetails, string[] Websites);
public sealed record MemberProfile(string Name, Guid Version, MemberPostalAddress? PostalAddress = null, ContactDetails? ContactDetails = null);
public interface IMemberProfileDirectory
{
    Task<MemberProfile?> ReadAsync(Guid contactId, CancellationToken ct);
    Task<MemberProfile?> UpdateNameAsync(Guid contactId, string name, Guid version, CancellationToken ct, MemberPostalAddress? postalAddress = null, bool updateAddress = false, ContactDetails? contactDetails = null, bool updateDetails = false);
    Task<MemberOrganisationAssociation?> ReadAssociationAsync(Guid accountId, Guid contactId, CancellationToken ct);
    Task<OrganisationProfile?> ReadOrganisationAsync(Guid accountId, CancellationToken ct);
}
