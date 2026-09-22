using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

internal static class AccountContactEndpoints
{
    public static void MapAccountContacts(this RouteGroupBuilder api)
    {
        api.MapGet("/account-contact-options", () => new {
            roles = AccountContactChoices.Roles, workClassifications = AccountContactChoices.WorkClassifications
        });
        api.MapGet("/records/{id:guid}/account-contacts", ListAsync);
        api.MapPost("/accounts/{accountId:guid}/contacts", CreateAsync).RequireAuthorization("crm.manage");
        api.MapPatch("/account-contacts/{id:guid}", UpdateAsync).RequireAuthorization("crm.manage");
        api.MapPost("/account-contacts/{id:guid}/end", EndAsync).RequireAuthorization("crm.manage");
    }

    private static async Task<IResult> ListAsync(Guid id, string? page, string? includeEnded, CrmDbContext db, CancellationToken ct)
    {
        if (!await db.Records.AnyAsync(x => x.Id == id, ct)) throw new KeyNotFoundException();
        var number = 1;
        Check(page == null || int.TryParse(page, out number), "Page must be a whole number.");
        Check(number is >= 1 and <= 1000000, "Page must be between 1 and 1000000.");
        var history = false;
        Check(includeEnded == null || bool.TryParse(includeEnded, out history), "Include ended must be true or false.");
        const int size = 25;
        var query = db.Set<AccountContact>().AsNoTracking().Where(x => x.AccountId == id || x.ContactId == id);
        if (!history) query = query.Where(x => x.EndDate == null);
        var totalCount = await query.CountAsync(ct);
        var items = await (from link in query
            join account in db.Records on link.AccountId equals account.Id
            join contact in db.Records on link.ContactId equals contact.Id
            orderby account.Name, contact.Name, link.Id
            select new { association = link, accountName = account.Name, contactName = contact.Name,
                accountArchived = account.IsArchived, contactArchived = contact.IsArchived })
            .Skip((number - 1) * size).Take(size).ToListAsync(ct);
        var ids = items.Select(x => x.association.Id).ToArray();
        var roles = await db.Set<AccountContactRole>().AsNoTracking().Where(x => ids.Contains(x.AssociationId))
            .OrderBy(x => x.RoleCode).ToListAsync(ct);
        return Results.Ok(new { items = items.Select(x => new {
            x.association, x.accountName, x.contactName, x.accountArchived, x.contactArchived,
            roles = roles.Where(r => r.AssociationId == x.association.Id).Select(r => r.RoleCode)
        }), totalCount, page = number, pageSize = size });
    }

    private static async Task<IResult> CreateAsync(Guid accountId, CreateAccountContactRequest r,
        CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var account = await Parent(db, accountId, "account", true, ct);
        var contact = await Parent(db, r.ContactId, "contact", true, ct);
        Validate(r.JobTitle, r.WorkClassification, r.Roles);
        Check(r.StartDate != default && r.StartDate.TimeOfDay == TimeSpan.Zero && r.StartDate.Date <= DateTime.UtcNow.Date,
            "Choose a start date no later than today.");
        if (await db.Set<AccountContact>().AnyAsync(x => x.AccountId == accountId && x.ContactId == r.ContactId && x.EndDate == null, ct))
            return Results.Problem(statusCode: 409, title: "This Contact already has a current link to this Account.");
        var link = new AccountContact { TenantId = who.TenantId!.Value, AccountId = accountId, ContactId = r.ContactId,
            StartDate = DateTime.SpecifyKind(r.StartDate.Date, DateTimeKind.Utc) };
        Apply(link, r.JobTitle, r.WorkClassification, r.IsMainContact, r.IsAuthorisedContact);
        db.Add(link);
        AddRoles(db, link, r.Roles!);
        Audit(db, who, http, account, contact, link.Id, "created");
        await db.SaveChangesAsync(ct);
        return Results.Ok(link);
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateAccountContactRequest r,
        CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var link = await db.Set<AccountContact>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (link.Version != r.Version) return Conflict();
        Check(link.EndDate == null, "Ended relationships are retained as history. Create a new link to rejoin.");
        var account = await Parent(db, link.AccountId, "account", true, ct);
        var contact = await Parent(db, link.ContactId, "contact", true, ct);
        Validate(r.JobTitle, r.WorkClassification, r.Roles);
        Apply(link, r.JobTitle, r.WorkClassification, r.IsMainContact, r.IsAuthorisedContact);
        var previous = await db.Set<AccountContactRole>().Where(x => x.AssociationId == id).ToListAsync(ct);
        db.RemoveRange(previous.Where(x => !r.Roles!.Contains(x.RoleCode)));
        AddRoles(db, link, r.Roles!.Except(previous.Select(x => x.RoleCode)));
        Audit(db, who, http, account, contact, link.Id, "updated");
        await db.SaveChangesAsync(ct);
        return Results.Ok(link);
    }

    private static async Task<IResult> EndAsync(Guid id, EndAccountContactRequest r,
        CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var link = await db.Set<AccountContact>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (link.Version != r.Version) return Conflict();
        Check(link.EndDate == null, "This relationship has already ended.");
        Check(r.EndDate.TimeOfDay == TimeSpan.Zero && r.EndDate.Date >= link.StartDate.Date && r.EndDate.Date <= DateTime.UtcNow.Date,
            "End date must be between the start date and today.");
        Check(!string.IsNullOrWhiteSpace(r.Reason) && r.Reason.Length <= 500, "Enter an end reason of 1–500 characters.");
        var account = await Parent(db, link.AccountId, "account", false, ct);
        var contact = await Parent(db, link.ContactId, "contact", false, ct);
        link.EndDate = DateTime.SpecifyKind(r.EndDate.Date, DateTimeKind.Utc);
        link.EndReason = r.Reason.Trim(); link.Version = Guid.NewGuid(); link.UpdatedAtUtc = DateTime.UtcNow;
        Audit(db, who, http, account, contact, link.Id, "ended");
        await db.SaveChangesAsync(ct);
        return Results.Ok(link);
    }

    private static async Task<CrmRecord> Parent(CrmDbContext db, Guid id, string kind, bool requireUnarchived, CancellationToken ct)
    {
        var record = await db.Records.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        Check(record.Kind == kind, $"Select a CRM {kind}.");
        Check(!requireUnarchived || !record.IsArchived, "Restore the archived record before changing its relationships.");
        return record;
    }
    private static void Validate(string? title, string classification, string[]? roles)
    {
        Check(title == null || title.Length <= 160, "Job title must not exceed 160 characters.");
        Check(AccountContactChoices.WorkClassifications.Contains(classification), "Choose a valid work classification.");
        Check(roles != null && roles.Length <= AccountContactChoices.Roles.Count && roles.Distinct().Count() == roles.Length &&
            roles.All(AccountContactChoices.Roles.Contains), "Choose distinct supported contact roles.");
    }
    private static void Apply(AccountContact link, string? title, string classification, bool main, bool authorised)
    {
        link.JobTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        link.WorkClassification = classification; link.IsMainContact = main; link.IsAuthorisedContact = authorised;
        link.Version = Guid.NewGuid(); link.UpdatedAtUtc = DateTime.UtcNow;
    }
    private static void AddRoles(CrmDbContext db, AccountContact link, IEnumerable<string> roles) =>
        db.AddRange(roles.Select(role => new AccountContactRole { TenantId = link.TenantId, AssociationId = link.Id, RoleCode = role }));
    private static void Audit(CrmDbContext db, IRequestIdentity who, HttpContext http, CrmRecord account, CrmRecord contact, Guid id, string action)
    {
        foreach (var record in new[] { account, contact })
        {
            // Both parent tokens participate in the transaction to detect concurrent archive/edit.
            record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
            db.Add(new Activity { TenantId = who.TenantId!.Value, RecordId = record.Id, ActorUserId = who.UserId!.Value,
                Action = $"account-contact.{action}:{id}", CorrelationId = http.TraceIdentifier });
        }
    }
    private static void Check(bool valid, string message) { if (!valid) throw new ValidationException(message); }
    private static IResult Conflict() => Results.Problem(statusCode: 409, title: "This relationship changed. Reload it before saving.");
}

public sealed record CreateAccountContactRequest(Guid ContactId, DateTime StartDate, string? JobTitle,
    string WorkClassification, bool IsMainContact, bool IsAuthorisedContact, string[]? Roles);
public sealed record UpdateAccountContactRequest(Guid Version, string? JobTitle,
    string WorkClassification, bool IsMainContact, bool IsAuthorisedContact, string[]? Roles);
public sealed record EndAccountContactRequest(Guid Version, DateTime EndDate, string Reason);
