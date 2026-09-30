namespace Nexora.Modules.Crm.Domain;

// A person's association with an organisation; it is not a login or membership.
public sealed class AccountContact : TenantRow
{
    public Guid AccountId { get; set; }
    public Guid ContactId { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string WorkClassification { get; set; } = "unknown";
    public bool IsMainAccount { get; set; }
    public bool IsMainContact { get; set; }
    public bool IsAuthorisedContact { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EndReason { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AccountContactRole : TenantRow
{
    public Guid AssociationId { get; set; }
    public string RoleCode { get; set; } = "";
}

public static class AccountContactChoices
{
    public static IReadOnlyList<string> Roles { get; } = Array.AsReadOnly(new[] {
        "assessment", "consultancy", "fumigation", "manufacturers-distributors", "pr",
        "servicing", "training", "events", "finance", "health-safety", "legal",
        "membership", "sales-marketing", "specifier", "technical"
    });
    public static IReadOnlyList<string> WorkClassifications { get; } =
        Array.AsReadOnly(new[] { "unknown", "technical", "non-technical" });
}
