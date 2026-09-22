using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Membership.Domain;
public sealed class MembershipProduct : TenantEntity
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int TermMonths { get; set; } = 12;
    public decimal Rate { get; set; }
    public string Currency { get; set; } = "GBP";
    public bool IsActive { get; set; } = true;
    public bool PortalApplicationsEnabled { get; set; }
    public bool PortalOrganisationApplicationsEnabled { get; set; }
    public string PortalTerms { get; set; } = "";
}
public sealed class PortalApplicationDraft : TenantEntity
{
    public Guid? AccountId { get; set; }
    public Guid ContactId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public string Notes { get; set; } = "";
    public Guid ProductVersion { get; set; }
    public Guid? PolicyVersion { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "GBP";
    public string Terms { get; set; } = "";
    public Guid? ApplicationId { get; set; }
}
public sealed class MembershipApplication : TenantEntity
{
    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = "";
    public Guid ProductId { get; set; }
    public string ApplicationType { get; set; } = "corporate";
    public string? ApplicantRole { get; set; }
    public bool TermsAccepted { get; set; }
    public DateTime? TermsAcceptedAtUtc { get; set; }
    public string? ApplicantNotes { get; set; }
    public string Status { get; set; } = "submitted";
    public DateTime RequestedStartUtc { get; set; }
    // Null on legacy applications: approval preserves the original term-month fallback.
    // New applications snapshot the quoted end date so later policy edits cannot change it.
    public DateTime? RequestedEndUtc { get; set; }
    public decimal QuotedRate { get; set; }
    public string Currency { get; set; } = "GBP";
    public int TermMonths { get; set; }
    public string DecisionReason { get; set; } = "";
    public Guid? MembershipId { get; set; }
}
public sealed class MemberSubscription : TenantEntity
{
    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = "";
    public Guid ProductId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public decimal Rate { get; set; }
    public string Currency { get; set; } = "GBP";
    public Guid ApplicationId { get; set; }
}
public sealed class MembershipHistory : TenantEntity, IAppendOnly
{
    public Guid SubjectId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTime? PreviousEndUtc { get; set; }
    public DateTime? NewEndUtc { get; set; }
}

