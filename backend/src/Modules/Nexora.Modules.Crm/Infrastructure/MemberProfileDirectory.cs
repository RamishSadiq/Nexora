using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Crm.Infrastructure;

internal sealed class MemberProfileDirectory(CrmDbContext db, IRequestIdentity who) : IMemberProfileDirectory
{
    public async Task<MemberProfile?> ReadAsync(Guid contactId, CancellationToken ct)
    {
        var record = await db.Records.Where(x => x.Id == contactId && x.Kind == "contact" && !x.IsArchived && x.Status == "active").SingleOrDefaultAsync(ct);
        if (record == null) return null;
        var address = await db.Set<Nexora.Modules.Crm.Domain.Address>().Where(a => a.RecordId == contactId && a.Label == "Member correspondence")
            .OrderBy(a => a.Id).Select(a => new MemberPostalAddress(a.Line1, a.Line2 ?? "", a.City, a.Region ?? "", a.PostalCode, a.Country)).FirstOrDefaultAsync(ct);
        return new(record.Name, record.Version, address, await RecordProfileFields.ContactAsync(db, contactId, ct));
    }
    public Task<MemberOrganisationAssociation?> ReadAssociationAsync(Guid accountId, Guid contactId, CancellationToken ct) =>
        db.Set<Nexora.Modules.Crm.Domain.AccountContact>()
            .Where(x => x.AccountId == accountId && x.ContactId == contactId && x.EndDate == null && x.StartDate <= DateTime.UtcNow.Date)
            .Select(x => new MemberOrganisationAssociation(x.Department, x.JobTitle, x.IsMainAccount)).SingleOrDefaultAsync(ct);
    public async Task<OrganisationProfile?> ReadOrganisationAsync(Guid accountId, CancellationToken ct)
    {
        var record = await db.Records.SingleOrDefaultAsync(x => x.Id == accountId && x.Kind == "account" && !x.IsArchived && x.Status == "active", ct);
        if (record == null) return null;
        return new(record.Name, await RecordProfileFields.AccountAsync(db, accountId, ct),
            await db.Set<Nexora.Modules.Crm.Domain.CommunicationMethod>().Where(x => x.RecordId == accountId && x.Kind == "website").OrderBy(x => x.Value).Select(x => x.Value).ToArrayAsync(ct));
    }
    public async Task<MemberProfile?> UpdateNameAsync(Guid contactId, string name, Guid version, CancellationToken ct, MemberPostalAddress? postalAddress = null, bool updateAddress = false, ContactDetails? contactDetails = null, bool updateDetails = false)
    {
        var record = await db.Records.SingleOrDefaultAsync(x => x.Id == contactId && x.Kind == "contact" && !x.IsArchived && x.Status == "active", ct);
        if (record == null) return null;
        if (record.Version != version) throw new DbUpdateConcurrencyException();
        if (updateAddress)
        {
            var addresses = await db.Set<Nexora.Modules.Crm.Domain.Address>().Where(a => a.RecordId == contactId && a.Label == "Member correspondence").ToListAsync(ct);
            if (postalAddress == null)
            {
                if (record.BillingUsesMailingAddress && addresses.Any(a => a.Purpose == "mailing"))
                    throw new ValidationException("This address is also used for billing. Ask staff to change the billing address before clearing it.");
                db.RemoveRange(addresses);
            }
            else
            {
                var address = addresses.OrderBy(a => a.Id).FirstOrDefault();
                if (address == null)
                {
                    address = new Nexora.Modules.Crm.Domain.Address { TenantId = record.TenantId, RecordId = contactId, Label = "Member correspondence" };
                    db.Add(address);
                }
                address.Line1 = postalAddress.Line1; address.Line2 = postalAddress.Line2; address.City = postalAddress.City;
                address.Region = postalAddress.Region; address.PostalCode = postalAddress.PostalCode; address.Country = postalAddress.Country;
            }
        }
        if (updateDetails) await RecordProfileFields.ApplyContactAsync(db, record, contactDetails, ct);
        record.Name = name;
        record.Version = Guid.NewGuid();
        record.UpdatedAtUtc = DateTime.UtcNow;
        db.Add(new Nexora.Modules.Crm.Domain.Activity { TenantId = record.TenantId, RecordId = record.Id,
            ActorUserId = who.UserId!.Value, Action = "portal.profile.updated", CorrelationId = Guid.NewGuid().ToString() });
        await db.SaveChangesAsync(ct);
        return await ReadAsync(contactId, ct);
    }
}
