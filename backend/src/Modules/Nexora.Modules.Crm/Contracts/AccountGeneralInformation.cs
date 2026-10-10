namespace Nexora.Modules.Crm.Contracts;

public sealed record AccountGeneralInformation(string? SourceAccountId = null, string? TradingStatus = null, string? AccountType = null,
    string? IndustrySector = null, string? Venue = null, string? MarketingSource = null, string? TurnoverRange = null,
    int? NumberOfEmployees = null, string? EmployeesRange = null, string? Region = null, string? Branch = null, string? OldReference = null);
