using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Documents;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
namespace Nexora.Modules.Crm;
public static partial class CrmModule
{
    private static void MapCompletion(RouteGroupBuilder api)
    {
        MapTransfer(api);
        api.MapGet("/records/{id:guid}/memberships", async (Guid id, int? page, int? pageSize, CrmDbContext db, ICrmMembershipDirectory memberships, CancellationToken ct) =>
        {
            var record = await RecordAsync(db, id, ct);
            var number = page ?? 1; var size = pageSize ?? 10;
            if (number is < 1 or > 10000 || size is < 1 or > 50) throw new ValidationException("Choose a page from 1 to 10000 and page size from 1 to 50.");
            return Results.Ok(await memberships.ReadAsync(id, record.Kind, number, size, ct));
        }).RequireAuthorization("membership.read");
        api.MapGet("/records/{id:guid}/context", async (Guid id, CrmDbContext db, IEnumerable<ICrmContextProvider> providers, IAuthorizationService auth, HttpContext http, CancellationToken ct) =>
        {
            var record = await RecordAsync(db, id, ct);
            var sections = new List<CrmContextSection>();
            foreach (var provider in providers)
            {
                var allowed = true;
                foreach (var permission in provider.Permissions)
                    if (!(await auth.AuthorizeAsync(http.User, null, permission)).Succeeded) { allowed = false; break; }
                if (allowed) sections.AddRange(await provider.ReadAsync(id, record.Kind, ct));
            }
            return Results.Ok(sections);
        });
        api.MapPost("/records/{id:guid}/files/{fileId:guid}/scan", async (Guid id, Guid fileId, TransitionRequest r, CrmDbContext db, IDocumentScanner scanner, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            var file = await db.Set<RecordFile>().SingleOrDefaultAsync(x => x.RecordId == id && x.Id == fileId, ct) ?? throw new KeyNotFoundException();
            if (file.Version != r.Version) return Conflict();
            if (file.ScanStatus != "quarantined") throw new ValidationException("Only quarantined files can be rescanned.");
            file.ScanStatus = await scanner.ScanAsync(file.Content, ct); file.Sha256 = Convert.ToHexString(SHA256.HashData(file.Content)); file.ScannedAtUtc = DateTime.UtcNow; file.Version = Guid.NewGuid();
            parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
            Audit(db, who, http, id, "file.scanned"); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("crm.manage", "crm.files");
        api.MapDelete("/records/{id:guid}/files/{fileId:guid}", async (Guid id, Guid fileId, [Microsoft.AspNetCore.Mvc.FromBody] TransitionRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var parent = await RecordAsync(db, id, ct, true);
            var file = await db.Set<RecordFile>().SingleOrDefaultAsync(x => x.RecordId == id && x.Id == fileId, ct) ?? throw new KeyNotFoundException();
            if (file.Version != r.Version) return Conflict();
            parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
            db.Remove(file); Audit(db, who, http, id, "file.removed"); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("crm.manage", "crm.files");
    }
}
