namespace Nexora.Modules.Crm.Domain;

public sealed class ContactProfile : RecordChild
{
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? PreviousLastName { get; set; }
    public string? PostNominal { get; set; }
    public string? SourcePersonId { get; set; }
    public string? ContactType { get; set; }
    public string? Nationality { get; set; }
    public string? OldReference { get; set; }
    public string? Region { get; set; }
    public string? Branch { get; set; }
    public string? Remarks { get; set; }
    public bool? ThirdPartyEmailPermission { get; set; }
    public bool? DisclosureDataToThirdParty { get; set; }
    public DateOnly? ThirdPartyDisclosureConsentDate { get; set; }
    public string? TypeOfThirdParty { get; set; }
    public bool? StopAllCommunication { get; set; }
    public DateOnly? DateStopAllCommunication { get; set; }
    public bool? BouncedInvalidEmailAddress { get; set; }
    public DateOnly? DateEmailBounced { get; set; }
    public long? NumberOfFollowers { get; set; }
    public long? NumberOfFollowing { get; set; }
    public long? NumberOfTweetsSent { get; set; }
    public long? NumberOfMentions { get; set; }
    public long? NumberOfTweetsWithTags { get; set; }
    public DateOnly? SocialMetricsObservedOn { get; set; }
    public string? SocialMetricsSource { get; set; }
}

public sealed class ContactSocialProfile : RecordChild
{
    public string Channel { get; set; } = "";
    public string Identifier { get; set; } = "";
}

public sealed class AccountProfile : RecordChild
{
    public string? SourceAccountId { get; set; }
    public string? TradingStatus { get; set; }
    public string? AccountType { get; set; }
    public string? IndustrySector { get; set; }
    public string? Venue { get; set; }
    public string? MarketingSource { get; set; }
    public string? TurnoverRange { get; set; }
    public int? NumberOfEmployees { get; set; }
    public string? EmployeesRange { get; set; }
    public string? Region { get; set; }
    public string? Branch { get; set; }
    public string? OldReference { get; set; }
    public string? FullName { get; set; }
    public string? TradingAs { get; set; }
    public DateOnly? DateBusinessCommenced { get; set; }
}
