using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;
namespace Nexora.Modules.Crm;
public static partial class CrmModule
{
    public sealed record ImportRequest(string Csv, bool Commit = false);
    private sealed record ImportResult(int Row, string Name, string Action, string? Error);
    private static readonly string[] TransferColumns = ["id", "version", "kind", "name", "status", "category", "email"];
    private static void MapTransfer(RouteGroupBuilder api)
    {
        api.MapGet("/export", async (string? kind, CrmDbContext db, CancellationToken ct) =>
        {
            Choice(kind, "contact", "account");
            var query = db.Records.Where(x => x.Kind == kind && !x.IsArchived);
            if (await query.CountAsync(ct) > 5000) throw new ValidationException("Exports support up to 5,000 current records per type.");
            var records = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync(ct);
            var emails = await db.Set<CommunicationMethod>().Where(x => x.Kind == "email" && query.Any(r => r.Id == x.RecordId))
                .OrderBy(x => x.Priority == "primary" ? 0 : x.Priority == "secondary" ? 1 : 2).ThenBy(x => x.Id).ToListAsync(ct);
            static string Cell(string? value)
            {
                value ??= "";
                if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            var csv = new StringBuilder(string.Join(',', TransferColumns)).AppendLine();
            foreach (var r in records) csv.AppendLine(string.Join(',', new[] { r.Id.ToString(), r.Version.ToString(), r.Kind, r.Name, r.Status, r.Category, emails.FirstOrDefault(e => e.RecordId == r.Id)?.Value }.Select(Cell)));
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", $"crm-{kind}.csv");
        }).RequireAuthorization("crm.manage");
        api.MapPost("/import", async (ImportRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Csv) || request.Csv.Length > 1024 * 1024) throw new ValidationException("Supply a CSV up to 1 MB and 500 records.");
            using var parser = new TextFieldParser(new StringReader(request.Csv.TrimStart('\ufeff'))) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
            parser.SetDelimiters(",");
            var rows = new List<string[]>();
            try
            {
                if (!(parser.ReadFields() ?? []).SequenceEqual(TransferColumns)) throw new ValidationException("Use columns: id,version,kind,name,status,category,email.");
                while (!parser.EndOfData) { if (rows.Count >= 500) throw new ValidationException("Import at most 500 records at a time."); rows.Add(parser.ReadFields() ?? []); }
            }
            catch (MalformedLineException) { throw new ValidationException("The CSV has malformed quoted fields."); }
            if (rows.Count == 0) throw new ValidationException("Add at least one data row.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var result = new List<ImportResult>(); var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var ids = new HashSet<Guid>(); var emailsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < rows.Count; i++)
            {
                var cells = rows[i]; var name = cells.Length > 3 ? cells[3] : "";
                try
                {
                    if (cells.Length != 7) throw new ValidationException("Expected seven columns.");
                    var kind = cells[2]; Choice(kind, "contact", "account"); name = Required(name, 160); Choice(cells[4], "active", "prospect", "inactive", "lapsed"); var category = Optional(cells[5], 100); var email = Optional(cells[6], 320);
                    if (!keys.Add(kind + ":" + name)) throw new ValidationException("Duplicate name and type in this CSV; reconcile before importing.");
                    if (email != null && (!new EmailAddressAttribute().IsValid(email) || !emailsInBatch.Add(email))) throw new ValidationException("Invalid or repeated email in this CSV.");
                    CrmRecord record; var creating = string.IsNullOrEmpty(cells[0]);
                    if (creating)
                    {
                        if (!string.IsNullOrEmpty(cells[1])) throw new ValidationException("New records must have an empty version.");
                        if (await db.Records.AnyAsync(x => x.Kind == kind && x.Name.ToLower() == name.ToLower(), ct)) throw new ValidationException("A matching record already exists, including archived records. Export and reconcile its ID/version.");
                        record = new CrmRecord { TenantId = who.TenantId!.Value, Kind = kind };
                    }
                    else
                    {
                        if (!Guid.TryParse(cells[0], out var id) || !ids.Add(id) || !Guid.TryParse(cells[1], out var version)) throw new ValidationException("Use a unique record ID and current version from an export.");
                        record = await db.Records.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ValidationException("Record is unavailable.");
                        if (record.Version != version || record.Kind != kind || record.IsArchived) throw new ValidationException("Record changed, has a different type, or is archived. Export again.");
                    }
                    CommunicationMethod? newEmail = null;
                    if (email != null)
                    {
                        if (await db.Set<CommunicationMethod>().AnyAsync(x => x.Kind == "email" && x.Value.ToLower() == email.ToLower() && x.RecordId != record.Id, ct)) throw new ValidationException("Email is already linked to another record. Reconcile it manually.");
                        var existing = await db.Set<CommunicationMethod>().Where(x => x.RecordId == record.Id && x.Kind == "email").ToListAsync(ct);
                        if (!existing.Any(x => string.Equals(x.Value, email, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (existing.Count > 0) throw new ValidationException("Update an existing email through the record so its consent evidence is preserved.");
                            newEmail = new CommunicationMethod { TenantId = record.TenantId, RecordId = record.Id, Kind = "email", Value = email, Consent = "unknown" };
                        }
                    }
                    if (request.Commit)
                    {
                        record.Name = name; record.Status = cells[4]; record.Category = category; record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
                        if (creating) db.Add(record); if (newEmail != null) db.Add(newEmail); Audit(db, who, http, record.Id, creating ? "record.imported" : "record.import-updated");
                    }
                    result.Add(new(i + 2, name, creating ? "create" : "update", null));
                }
                catch (ValidationException e) { result.Add(new(i + 2, name, "blocked", e.Message)); }
            }
            var valid = result.All(x => x.Error == null);
            if (request.Commit && valid) { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
            return Results.Ok(new { valid, committed = request.Commit && valid, rows = result });
        }).RequireAuthorization("crm.manage");
    }
}
