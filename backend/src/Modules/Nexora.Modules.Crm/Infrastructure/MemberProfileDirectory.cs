using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Crm.Infrastructure;

internal sealed class MemberProfileDirectory(CrmDbContext db, IRequestIdentity who) : IMemberProfileDirectory
{
    public Task<MemberProfile?> ReadAsync(Guid contactId, CancellationToken ct) => db.Records
        .Where(x => x.Id == contactId && x.Kind == "contact" && !x.IsArchived && x.Status == "active")
        .Select(x => new MemberProfile(x.Name, x.Version)).SingleOrDefaultAsync(ct);
    public async Task<MemberProfile?> UpdateNameAsync(Guid contactId, string name, Guid version, CancellationToken ct)
    {
        var record = await db.Records.SingleOrDefaultAsync(x => x.Id == contactId && x.Kind == "contact" && !x.IsArchived && x.Status == "active", ct);
        if (record == null) return null;
        if (record.Version != version) throw new DbUpdateConcurrencyException();
        record.Name = name;
        record.Version = Guid.NewGuid();
        record.UpdatedAtUtc = DateTime.UtcNow;
        db.Add(new Nexora.Modules.Crm.Domain.Activity { TenantId = record.TenantId, RecordId = record.Id,
            ActorUserId = who.UserId!.Value, Action = "portal.profile.updated", CorrelationId = Guid.NewGuid().ToString() });
        await db.SaveChangesAsync(ct);
        return new MemberProfile(record.Name, record.Version);
    }
}
