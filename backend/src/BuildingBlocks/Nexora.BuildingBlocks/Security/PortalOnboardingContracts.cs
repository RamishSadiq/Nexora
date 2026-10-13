using System.Data.Common;

namespace Nexora.BuildingBlocks.Security;

public sealed record PortalOnboarding(bool AllowSelfRegistration = false, bool AllowOrganisationRequests = false);
public sealed record PortalRegistrationSite(Guid TenantId, bool AllowSelfRegistration);
public interface IPortalRegistrationDirectory
{
    Task<PortalRegistrationSite?> ResolveAsync(CancellationToken ct);
}

// CRM owns these writes. The caller supplies its transaction so a login/contact or
// approved relationship/grant cannot be left partially persisted.
public interface IPortalOnboardingCrm
{
    Task CreateContactAsync(Guid tenantId, Guid userId, Guid contactId, string firstName, string lastName,
        string email, DbConnection connection, DbTransaction transaction, CancellationToken ct);
    Task AssociateAsync(Guid tenantId, Guid reviewerId, Guid contactId, Guid accountId, string jobTitle,
        DbConnection connection, DbTransaction transaction, CancellationToken ct);
}
