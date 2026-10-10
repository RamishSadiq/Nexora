namespace Nexora.Modules.Crm.Contracts;

public sealed record ContactGeneralInformation(
    string? SourcePersonId = null,
    string? ContactType = null,
    string? Nationality = null,
    string? OldReference = null,
    string? Region = null,
    string? Branch = null,
    string? Remarks = null);
