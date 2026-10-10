namespace Nexora.Modules.Crm.Contracts;

public sealed record ContactCommunicationPermissions(
    bool? ThirdPartyEmailPermission = null,
    bool? DisclosureDataToThirdParty = null,
    DateOnly? ThirdPartyDisclosureConsentDate = null,
    string? TypeOfThirdParty = null,
    bool? StopAllCommunication = null,
    DateOnly? DateStopAllCommunication = null,
    bool? BouncedInvalidEmailAddress = null,
    DateOnly? DateEmailBounced = null);
