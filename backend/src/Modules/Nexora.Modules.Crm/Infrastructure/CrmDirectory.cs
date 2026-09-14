using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Crm.Domain;
using Nexora.BuildingBlocks.Security;
namespace Nexora.Modules.Crm.Infrastructure;
internal sealed class CrmDirectory(CrmDbContext db) : ICrmDirectory
{
    public async Task<string?> MarketingEmailAsync(Guid contactId, CancellationToken ct)
    {
        if (!await db.Records.AnyAsync(x=>x.Id==contactId&&x.Kind=="contact"&&!x.IsArchived,ct)) return null;
        if (await db.Set<CommunicationMethod>().AnyAsync(x=>x.RecordId==contactId&&x.Kind=="email"&&x.Consent=="denied",ct)) return null;
        return await db.Set<CommunicationMethod>().Where(x=>x.RecordId==contactId&&x.Kind=="email"&&x.Consent=="granted"&&x.ConsentEvidence!=null)
            .OrderBy(x=>x.Id).Select(x=>x.Value).FirstOrDefaultAsync(ct);
    }
    public async Task<IReadOnlyList<CrmIdentity>> SearchContactsAsync(string? search, CancellationToken ct)
    {
        var query = db.Records.Where(x => x.Kind == "contact" && !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search));
        return await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Take(50).Select(x => new CrmIdentity(x.Id,x.Name,x.Kind)).ToListAsync(ct);
    }
    public Task<CrmIdentity?> FindActiveAsync(Guid id, CancellationToken ct) => db.Records.Where(x => x.Id == id && !x.IsArchived)
        .Select(x => new CrmIdentity(x.Id, x.Name, x.Kind)).SingleOrDefaultAsync(ct);
}
