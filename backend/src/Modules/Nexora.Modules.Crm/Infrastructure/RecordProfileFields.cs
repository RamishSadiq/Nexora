using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Contracts;

namespace Nexora.Modules.Crm.Infrastructure;

internal static class RecordProfileFields
{
    private static string? Text(string? value)
    {
        value = value?.Trim();
        if (value?.Length > 160 || value?.Any(char.IsControl) == true)
            throw new ValidationException("Profile fields must be at most 160 characters without control characters.");
        return string.IsNullOrEmpty(value) ? null : value;
    }

    internal static Task<ContactDetails?> ContactAsync(CrmDbContext db, Guid id, CancellationToken ct) =>
        db.Set<ContactProfile>().Where(x => x.RecordId == id)
            .Select(x => new ContactDetails(x.Title, x.FirstName, x.MiddleName, x.LastName, x.PreviousLastName, x.PostNominal)).SingleOrDefaultAsync(ct);

    internal static Task<AccountDetails?> AccountAsync(CrmDbContext db, Guid id, CancellationToken ct) =>
        db.Set<AccountProfile>().Where(x => x.RecordId == id)
            .Select(x => new AccountDetails(x.FullName, x.TradingAs, x.DateBusinessCommenced)).SingleOrDefaultAsync(ct);

    internal static Task<AccountGeneralInformation?> AccountGeneralInformationAsync(CrmDbContext db, Guid id, CancellationToken ct) =>
        db.Set<AccountProfile>().Where(x => x.RecordId == id).Select(x => new AccountGeneralInformation(x.SourceAccountId, x.TradingStatus, x.AccountType, x.IndustrySector, x.Venue,
            x.MarketingSource, x.TurnoverRange, x.NumberOfEmployees, x.EmployeesRange, x.Region, x.Branch, x.OldReference)).SingleOrDefaultAsync(ct);

    internal static async Task ApplyAccountGeneralInformationAsync(CrmDbContext db, CrmRecord record, AccountGeneralInformation details, CancellationToken ct)
    {
        if (details.NumberOfEmployees < 0) throw new ValidationException("Number of employees must be a nonnegative whole number, or left unknown.");
        var clean = new AccountGeneralInformation(Text(details.SourceAccountId), Text(details.TradingStatus), Text(details.AccountType), Text(details.IndustrySector), Text(details.Venue),
            Text(details.MarketingSource), Text(details.TurnoverRange), details.NumberOfEmployees, Text(details.EmployeesRange), Text(details.Region), Text(details.Branch), Text(details.OldReference));
        var row = db.Set<AccountProfile>().Local.SingleOrDefault(x => x.RecordId == record.Id) ?? await db.Set<AccountProfile>().SingleOrDefaultAsync(x => x.RecordId == record.Id, ct);
        if (row == null) { row = new AccountProfile { TenantId = record.TenantId, RecordId = record.Id }; db.Add(row); }
        row.SourceAccountId = clean.SourceAccountId; row.TradingStatus = clean.TradingStatus; row.AccountType = clean.AccountType; row.IndustrySector = clean.IndustrySector;
        row.Venue = clean.Venue; row.MarketingSource = clean.MarketingSource; row.TurnoverRange = clean.TurnoverRange; row.NumberOfEmployees = clean.NumberOfEmployees;
        row.EmployeesRange = clean.EmployeesRange; row.Region = clean.Region; row.Branch = clean.Branch; row.OldReference = clean.OldReference;
    }

    internal static Task<ContactGeneralInformation?> GeneralInformationAsync(CrmDbContext db, Guid id, CancellationToken ct) =>
        db.Set<ContactProfile>().Where(x => x.RecordId == id)
            .Select(x => new ContactGeneralInformation(x.SourcePersonId, x.ContactType, x.Nationality, x.OldReference, x.Region, x.Branch, x.Remarks)).SingleOrDefaultAsync(ct);

    internal static async Task ApplyGeneralInformationAsync(CrmDbContext db, CrmRecord record, ContactGeneralInformation details, CancellationToken ct)
    {
        var remarks = details.Remarks?.Trim();
        if (remarks?.Length > 4000 || remarks?.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t') == true)
            throw new ValidationException("Remarks must be at most 4000 characters without unsupported control characters.");
        var clean = new ContactGeneralInformation(Text(details.SourcePersonId), Text(details.ContactType), Text(details.Nationality), Text(details.OldReference), Text(details.Region), Text(details.Branch), string.IsNullOrEmpty(remarks) ? null : remarks);
        var row = db.Set<ContactProfile>().Local.SingleOrDefault(x => x.RecordId == record.Id)
            ?? await db.Set<ContactProfile>().SingleOrDefaultAsync(x => x.RecordId == record.Id, ct);
        if (row == null) { row = new ContactProfile { TenantId = record.TenantId, RecordId = record.Id }; db.Add(row); }
        row.SourcePersonId = clean.SourcePersonId; row.ContactType = clean.ContactType;
        row.Nationality = clean.Nationality; row.OldReference = clean.OldReference;
        row.Region = clean.Region; row.Branch = clean.Branch; row.Remarks = clean.Remarks;
    }

    internal static Task<ContactCommunicationPermissions?> CommunicationPermissionsAsync(CrmDbContext db, Guid id, CancellationToken ct) =>
        db.Set<ContactProfile>().Where(x => x.RecordId == id)
            .Select(x => new ContactCommunicationPermissions(x.ThirdPartyEmailPermission, x.DisclosureDataToThirdParty, x.ThirdPartyDisclosureConsentDate, x.TypeOfThirdParty, x.StopAllCommunication, x.DateStopAllCommunication, x.BouncedInvalidEmailAddress, x.DateEmailBounced)).SingleOrDefaultAsync(ct);

    internal static async Task ApplyCommunicationPermissionsAsync(CrmDbContext db, CrmRecord record, ContactCommunicationPermissions details, CancellationToken ct)
    {
        foreach (var date in new[] { details.ThirdPartyDisclosureConsentDate, details.DateStopAllCommunication, details.DateEmailBounced })
            if (date == DateOnly.MinValue || date > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ValidationException("Communication permission dates must be valid dates on or before today.");
        var thirdPartyType = Text(details.TypeOfThirdParty);
        var row = db.Set<ContactProfile>().Local.SingleOrDefault(x => x.RecordId == record.Id)
            ?? await db.Set<ContactProfile>().SingleOrDefaultAsync(x => x.RecordId == record.Id, ct);
        if (row == null) { row = new ContactProfile { TenantId = record.TenantId, RecordId = record.Id }; db.Add(row); }
        row.ThirdPartyEmailPermission = details.ThirdPartyEmailPermission;
        row.DisclosureDataToThirdParty = details.DisclosureDataToThirdParty;
        row.ThirdPartyDisclosureConsentDate = details.ThirdPartyDisclosureConsentDate;
        row.TypeOfThirdParty = thirdPartyType;
        row.StopAllCommunication = details.StopAllCommunication;
        row.DateStopAllCommunication = details.DateStopAllCommunication;
        row.BouncedInvalidEmailAddress = details.BouncedInvalidEmailAddress;
        row.DateEmailBounced = details.DateEmailBounced;
    }

    internal static async Task ApplyContactAsync(CrmDbContext db, CrmRecord record, ContactDetails? details, CancellationToken ct)
    {
        var clean = new ContactDetails(Text(details?.Title), Text(details?.FirstName), Text(details?.MiddleName), Text(details?.LastName), Text(details?.PreviousLastName), Text(details?.PostNominal));
        var row = await db.Set<ContactProfile>().SingleOrDefaultAsync(x => x.RecordId == record.Id, ct);
        if (row == null) { row = new ContactProfile { TenantId = record.TenantId, RecordId = record.Id }; db.Add(row); }
        row.Title = clean.Title; row.FirstName = clean.FirstName; row.MiddleName = clean.MiddleName;
        row.LastName = clean.LastName; row.PreviousLastName = clean.PreviousLastName; row.PostNominal = clean.PostNominal;
    }

    internal static async Task ApplyAccountAsync(CrmDbContext db, CrmRecord record, AccountDetails? details, CancellationToken ct)
    {
        var fullName = Text(details?.FullName); var tradingAs = Text(details?.TradingAs);
        if (details?.DateBusinessCommenced > DateOnly.FromDateTime(DateTime.UtcNow) || details?.DateBusinessCommenced == DateOnly.MinValue)
            throw new ValidationException("Enter a valid business commencement date on or before today.");
        var row = await db.Set<AccountProfile>().SingleOrDefaultAsync(x => x.RecordId == record.Id, ct);
        if (row == null) { row = new AccountProfile { TenantId = record.TenantId, RecordId = record.Id }; db.Add(row); }
        row.FullName = fullName; row.TradingAs = tradingAs; row.DateBusinessCommenced = details?.DateBusinessCommenced;
    }
}
