using Nexora.BuildingBlocks.Domain;
namespace Nexora.Modules.Membership.Domain;

public sealed class MembershipPolicy : TenantEntity
{
    public Guid ProductId { get; set; }
    public string RenewalMode { get; set; } = "rolling";
    public int YearEndMonth { get; set; } = 12;
    public int YearEndDay { get; set; } = 31;
    public bool Prorate { get; set; }
    public decimal ProcessingFee { get; set; }
    public decimal JoiningFee { get; set; }
    public decimal Credit { get; set; }
    public bool RequirePayment { get; set; }
    public bool RequireCompliance { get; set; }
    public bool RequireDeclaration { get; set; }
}

public sealed class MembershipReview : TenantEntity
{
    public Guid MembershipId { get; set; }
    public DateTime TermEndUtc { get; set; }
    public bool PaymentVerified { get; set; }
    public string PaymentReference { get; set; } = "";
    public string ComplianceStatus { get; set; } = "pending";
    public string Evidence { get; set; } = "";
    public bool DeclarationAccepted { get; set; }
    public bool ExcludedFromRenewal { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class MembershipCertificate : TenantEntity, IAppendOnly
{
    public Guid MembershipId { get; set; }
    public string Number { get; set; } = "";
    public string HolderName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public Guid IssuedByUserId { get; set; }
}

public sealed class MembershipCommunication : TenantEntity, IAppendOnly
{
    public Guid MembershipId { get; set; }
    public string Kind { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "draft";
    public Guid RequestId { get; set; }
    public Guid CreatedByUserId { get; set; }
}
