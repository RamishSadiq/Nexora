using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;

namespace Nexora.Modules.Crm.Infrastructure;

internal sealed class PortalOnboardingCrm(DbContextOptions<CrmDbContext> options) : IPortalOnboardingCrm
{
    private sealed record OnboardingIdentity(Guid Tenant, Guid User) : IRequestIdentity
    {
        public Guid? TenantId => Tenant;
        public Guid? UserId => User;
    }
    private async Task<CrmDbContext> Context(Guid tenantId, Guid actorId, DbConnection connection, DbTransaction transaction, CancellationToken ct)
    {
        var db = new CrmDbContext(options, new OnboardingIdentity(tenantId, actorId));
        db.Database.SetDbConnection(connection);
        await db.Database.UseTransactionAsync(transaction, ct);
        return db;
    }
    public async Task CreateContactAsync(Guid tenantId, Guid userId, Guid contactId, string firstName, string lastName, string email,
        DbConnection connection, DbTransaction transaction, CancellationToken ct)
    {
        await using var db = await Context(tenantId, userId, connection, transaction, ct);
        db.Records.Add(new CrmRecord { Id = contactId, TenantId = tenantId, Kind = "contact", Name = $"{firstName} {lastName}", Status = "active" });
        await db.SaveChangesAsync(ct);
        db.Add(new ContactProfile { TenantId = tenantId, RecordId = contactId, FirstName = firstName, LastName = lastName });
        db.Add(new CommunicationMethod { TenantId = tenantId, RecordId = contactId, Kind = "email", Priority = "primary", Value = email, Consent = "unknown" });
        db.Add(new Activity { TenantId = tenantId, RecordId = contactId, ActorUserId = userId, Action = "Created through portal self-registration" });
        await db.SaveChangesAsync(ct);
    }
    public async Task AssociateAsync(Guid tenantId, Guid reviewerId, Guid contactId, Guid accountId, string jobTitle,
        DbConnection connection, DbTransaction transaction, CancellationToken ct)
    {
        await using var db = await Context(tenantId, reviewerId, connection, transaction, ct);
        if (!await db.Records.AnyAsync(x => x.Id == accountId && x.Kind == "account" && x.Status == "active" && !x.IsArchived, ct) ||
            !await db.Records.AnyAsync(x => x.Id == contactId && x.Kind == "contact" && x.Status == "active" && !x.IsArchived, ct))
            throw new ValidationException("Choose an active organisation and member contact in this association.");
        var today = DateTime.UtcNow.Date;
        if (!await db.Set<AccountContact>().AnyAsync(x => x.AccountId == accountId && x.ContactId == contactId && x.StartDate <= today && x.EndDate == null, ct))
            db.Add(new AccountContact { TenantId = tenantId, AccountId = accountId, ContactId = contactId, JobTitle = jobTitle, StartDate = today });
        db.Add(new Activity { TenantId = tenantId, RecordId = contactId, ActorUserId = reviewerId, Action = "Portal organisation access request approved" });
        await db.SaveChangesAsync(ct);
    }
}
