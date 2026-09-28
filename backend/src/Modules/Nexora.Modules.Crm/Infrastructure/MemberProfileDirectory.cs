using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Crm.Infrastructure;

internal sealed class MemberProfileDirectory(CrmDbContext db, IRequestIdentity who) : IMemberProfileDirectory
{
    public Task<MemberProfile?> ReadAsync(Guid contactId, CancellationToken ct) => db.Records
        .Where(x => x.Id == contactId && x.Kind == "contact" && !x.IsArchived && x.Status == "active")
        .Select(x => new MemberProfile(x.Name, x.Version, db.Set<Nexora.Modules.Crm.Domain.Address>()
            .Where(a => a.RecordId == x.Id && a.Label == "Member correspondence")
            .Select(a => new MemberPostalAddress(a.Line1, a.Line2 ?? "", a.City, a.Region ?? "", a.PostalCode, a.Country)).FirstOrDefault())).SingleOrDefaultAsync(ct);
    public async Task<MemberProfile?> UpdateNameAsync(Guid contactId, string name, Guid version, CancellationToken ct, MemberPostalAddress? postalAddress = null, bool updateAddress = false)
    {
        var record = await db.Records.SingleOrDefaultAsync(x => x.Id == contactId && x.Kind == "contact" && !x.IsArchived && x.Status == "active", ct);
        if (record == null) return null;
        if (record.Version != version) throw new DbUpdateConcurrencyException();
        if (updateAddress)
        {
            var addresses = await db.Set<Nexora.Modules.Crm.Domain.Address>().Where(a => a.RecordId == contactId && a.Label == "Member correspondence").ToListAsync(ct);
            db.RemoveRange(addresses);
            if (postalAddress != null) db.Add(new Nexora.Modules.Crm.Domain.Address {
                TenantId = record.TenantId, RecordId = contactId, Label = "Member correspondence",
                Line1 = postalAddress.Line1, Line2 = postalAddress.Line2, City = postalAddress.City,
                Region = postalAddress.Region, PostalCode = postalAddress.PostalCode, Country = postalAddress.Country });
        }
        record.Name = name;
        record.Version = Guid.NewGuid();
        record.UpdatedAtUtc = DateTime.UtcNow;
        db.Add(new Nexora.Modules.Crm.Domain.Activity { TenantId = record.TenantId, RecordId = record.Id,
            ActorUserId = who.UserId!.Value, Action = "portal.profile.updated", CorrelationId = Guid.NewGuid().ToString() });
        await db.SaveChangesAsync(ct);
        return await ReadAsync(contactId, ct);
    }
}
