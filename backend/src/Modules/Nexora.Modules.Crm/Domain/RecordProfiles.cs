namespace Nexora.Modules.Crm.Domain;

public sealed class ContactProfile : RecordChild
{
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? PreviousLastName { get; set; }
    public string? PostNominal { get; set; }
}

public sealed class AccountProfile : RecordChild
{
    public string? FullName { get; set; }
    public string? TradingAs { get; set; }
    public DateOnly? DateBusinessCommenced { get; set; }
}
