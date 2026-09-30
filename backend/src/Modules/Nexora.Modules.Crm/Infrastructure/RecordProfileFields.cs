using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;

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
