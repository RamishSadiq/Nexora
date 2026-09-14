using Nexora.BuildingBlocks.Reporting;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Security;
using Nexora.Modules.Crm.Contracts;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

public static class CrmModule
{
    public static IServiceCollection AddNexoraCrm(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICrmDirectory, CrmDirectory>();
        services.AddDbContext<CrmDbContext>(o => o.UseSqlServer(configuration.GetConnectionString("Nexora"),
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "crm")));
        services.AddScoped<IReportDataset>(sp=>new ReportDataset("contacts","Contacts","crm.read",sp.GetRequiredService<CrmDbContext>().Set<CrmRecord>().AsNoTracking().Where(x=>x.Kind=="contact"&&!x.IsArchived).Select(x=>new ReportRow{Id=x.Id,Label=x.Name,Status=x.Status,CreatedAtUtc=x.CreatedAtUtc})));
        return services;
    }
    public static IEndpointRouteBuilder MapNexoraCrm(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1/crm").WithTags("CRM").RequireAuthorization("crm.read");
        api.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            try
            {
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
                return await next(context);
            }
            catch (AntiforgeryValidationException) { return Results.Problem(statusCode: 400, title: "The security token is invalid or expired."); }
            catch (ValidationException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] }); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 409, title: "This record changed. Reload it before saving again."); }
            catch (CrmBoundaryException) { return Results.Forbid(); }
            catch (DbUpdateException) { return Results.Problem(statusCode: 409, title: "The change conflicts with existing data. Reload and check related records."); }
        });
        api.MapGet("/directory", (IIdentityDirectory directory, CancellationToken ct) => directory.GetAsync(ct));
        api.MapGet("/records", ListAsync);
        api.MapGet("/records/{id:guid}", DetailAsync);
        api.MapPost("/records", CreateAsync).RequireAuthorization("crm.manage");
        api.MapPatch("/records/{id:guid}", UpdateAsync).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/archive", (Guid id, TransitionRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) => TransitionAsync(id, request, true, db, who, http, ct)).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/restore", (Guid id, TransitionRequest request, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) => TransitionAsync(id, request, false, db, who, http, ct)).RequireAuthorization("crm.manage");
        api.MapGet("/records/{id:guid}/timeline", async (Guid id, CrmDbContext db, int? page, CancellationToken ct) =>
        {
            await RecordAsync(db, id, ct);
            var offset = Math.Clamp(page ?? 1, 1, 10000);
            return Results.Ok(await db.Set<Activity>().Where(x => x.RecordId == id).OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((offset - 1) * 50).Take(50).ToListAsync(ct));
        });
        api.MapPost("/records/{id:guid}/addresses", async (Guid id, AddressRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var row = new Address { Label = Required(r.Label, 80), Line1 = Required(r.Line1, 200), Line2 = Optional(r.Line2, 200), City = Required(r.City, 100), Region = Optional(r.Region, 100), PostalCode = Required(r.PostalCode, 30), Country = Required(r.Country, 80) };
            return await AddChildAsync(id, row, "address.added", db, who, http, ct);
        }).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/communications", async (Guid id, CommunicationRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            Choice(r.Kind, "email", "phone", "website"); Choice(r.Consent, "unknown", "granted", "denied");
            var value = Required(r.Value, 320);
            if (r.Kind == "email" && !new EmailAddressAttribute().IsValid(value)) throw new ValidationException("Enter a valid email address.");
            if (r.Kind == "website" && (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))) throw new ValidationException("Enter an HTTP or HTTPS website.");
            var evidence = r.Consent == "unknown" ? Optional(r.ConsentEvidence, 500) : Required(r.ConsentEvidence, 500);
            return await AddChildAsync(id, new CommunicationMethod { Kind = r.Kind, Value = value, Consent = r.Consent, ConsentEvidence = evidence }, "communication.added", db, who, http, ct);
        }).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/relationships", async (Guid id, RelationshipRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var target = await RecordAsync(db, r.TargetRecordId, ct, true);
            if (target.Id == id) throw new ValidationException("Choose a different related record.");
            return await AddChildAsync(id, new RecordRelationship { TargetRecordId = target.Id, Label = Required(r.Label, 100) }, "relationship.added", db, who, http, ct);
        }).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/notes", async (Guid id, NoteRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
            await AddChildAsync(id, new Note { Body = Required(r.Body, 4000), ActorUserId = who.UserId!.Value }, "note.added", db, who, http, ct)).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/tags", async (Guid id, TagRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
            await AddChildAsync(id, new RecordTag { Name = Required(r.Name, 80).ToLowerInvariant() }, "tag.added", db, who, http, ct)).RequireAuthorization("crm.manage");
        api.MapDelete("/records/{id:guid}/{collection}/{childId:guid}", DeleteChildAsync).RequireAuthorization("crm.manage");
        api.MapPost("/records/{id:guid}/files", UploadAsync).RequireAuthorization("crm.manage", "crm.files");
        api.MapGet("/records/{id:guid}/files/{fileId:guid}", async (Guid id, Guid fileId, CrmDbContext db, HttpContext http, CancellationToken ct) =>
        {
            await RecordAsync(db, id, ct);
            var file = await db.Set<RecordFile>().SingleOrDefaultAsync(x => x.RecordId == id && x.Id == fileId, ct) ?? throw new KeyNotFoundException();
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(file.Content, "application/octet-stream", file.Name);
        }).RequireAuthorization("crm.files");
        api.MapGet("/fields", async (CrmDbContext db, CancellationToken ct) => await db.Set<CustomFieldDefinition>().OrderBy(x => x.Name).ToListAsync(ct));
        api.MapPost("/fields", async (FieldRequest r, CrmDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            Choice(r.Kind, "contact", "account"); Choice(r.DataType, "text", "number", "boolean", "date");
            if (await db.Set<CustomFieldDefinition>().CountAsync(ct) >= 50) throw new ValidationException("A tenant can define up to 50 CRM fields.");
            var field = new CustomFieldDefinition { TenantId = who.TenantId!.Value, Name = Required(r.Name, 80), Kind = r.Kind, DataType = r.DataType };
            db.Add(field); await db.SaveChangesAsync(ct); return Results.Ok(field);
        }).RequireAuthorization("crm.configure");
        api.MapPut("/records/{id:guid}/fields/{fieldId:guid}", SetFieldAsync).RequireAuthorization("crm.manage");
        api.MapGet("/views", async (CrmDbContext db, CancellationToken ct) => await db.Set<SavedView>().OrderBy(x => x.Name).ToListAsync(ct));
        api.MapPost("/views", async (ViewRequest r, CrmDbContext db, IRequestIdentity who, CancellationToken ct) =>
        {
            Choice(r.Kind, "contact", "account"); Choice(r.Sort, "name", "-name", "updated"); Choice(r.Status, "", "active", "prospect", "inactive");
            Required(r.Columns, 100);
            foreach (var column in r.Columns.Split(',')) Choice(column, "name", "status", "category", "updatedAtUtc");
            if (!r.Columns.Split(',').Contains("name")) throw new ValidationException("The name column is required.");
            var row = new SavedView { TenantId = who.TenantId!.Value, UserId = who.UserId!.Value, Name = Required(r.Name, 80), Kind = r.Kind, Search = Optional(r.Search, 160) ?? "", Status = r.Status, Sort = r.Sort, Archived = r.Archived, Columns = Required(r.Columns, 100) };
            db.Add(row); await db.SaveChangesAsync(ct); return Results.Ok(row);
        });
        api.MapDelete("/views/{id:guid}", async (Guid id, CrmDbContext db, CancellationToken ct) =>
        {
            var row = await db.Set<SavedView>().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            db.Remove(row); await db.SaveChangesAsync(ct); return Results.NoContent();
        });
        return endpoints;
    }

    private static async Task<IResult> ListAsync(CrmDbContext db, string? kind, string? search, string? status, string? sort, bool? archived, int? page, int? pageSize, CancellationToken ct)
    {
        kind ??= "contact"; sort ??= "name";
        Choice(kind, "contact", "account"); Choice(sort, "name", "-name", "updated");
        if (!string.IsNullOrEmpty(status)) Choice(status, "active", "prospect", "inactive");
        var term = Optional(search, 160);
        var query = db.Records.AsNoTracking().Where(x => x.Kind == kind && x.IsArchived == (archived ?? false));
        if (!string.IsNullOrEmpty(term)) query = query.Where(x => x.Name.Contains(term));
        if (!string.IsNullOrEmpty(status)) query = query.Where(x => x.Status == status);
        var count = await query.CountAsync(ct);
        var ordered = sort switch { "-name" => query.OrderByDescending(x => x.Name), "updated" => query.OrderByDescending(x => x.UpdatedAtUtc), _ => query.OrderBy(x => x.Name) };
        var number = Math.Clamp(page ?? 1, 1, 10000); var size = Math.Clamp(pageSize ?? 20, 1, 100);
        return Results.Ok(new { items = await ordered.ThenBy(x => x.Id).Skip((number - 1) * size).Take(size).ToListAsync(ct), total = count, page = number, pageSize = size });
    }
    private static async Task<IResult> DetailAsync(Guid id, CrmDbContext db, CancellationToken ct)
    {
        var record = await RecordAsync(db, id, ct);
        return Results.Ok(new
        {
            record,
            addresses = await db.Set<Address>().Where(x => x.RecordId == id).ToListAsync(ct),
            communications = await db.Set<CommunicationMethod>().Where(x => x.RecordId == id).ToListAsync(ct),
            relationships = await (from link in db.Set<RecordRelationship>() join target in db.Records on link.TargetRecordId equals target.Id where link.RecordId == id select new { link.Id, link.Label, link.TargetRecordId, target.Name }).ToListAsync(ct),
            notes = await db.Set<Note>().Where(x => x.RecordId == id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct),
            tags = await db.Set<RecordTag>().Where(x => x.RecordId == id).OrderBy(x => x.Name).ToListAsync(ct),
            files = await db.Set<RecordFile>().Where(x => x.RecordId == id).Select(x => new { x.Id, x.Name, x.CreatedAtUtc }).ToListAsync(ct),
            fields = await db.Set<CustomFieldValue>().Where(x => x.RecordId == id).ToListAsync(ct)
        });
    }
    private static async Task<IResult> CreateAsync(RecordRequest r, CrmDbContext db, IIdentityDirectory directory, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        await ValidateAsync(r, directory, ct);
        var row = new CrmRecord { TenantId = who.TenantId!.Value, Kind = r.Kind };
        Apply(row, r); db.Add(row); Audit(db, who, http, row.Id, "record.created");
        await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/crm/records/{row.Id}", row);
    }
    private static async Task<IResult> UpdateAsync(Guid id, RecordRequest r, CrmDbContext db, IIdentityDirectory directory, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var row = await RecordAsync(db, id, ct, true);
        if (r.Version != row.Version) return Conflict();
        if (r.Kind != row.Kind) throw new ValidationException("Record type cannot change.");
        await ValidateAsync(r, directory, ct); Apply(row, r);
        Audit(db, who, http, id, "record.updated"); await db.SaveChangesAsync(ct); return Results.Ok(row);
    }
    private static async Task<IResult> TransitionAsync(Guid id, TransitionRequest r, bool archived, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var row = await RecordAsync(db, id, ct);
        if (r.Version != row.Version) return Conflict();
        row.IsArchived = archived; row.Version = Guid.NewGuid(); row.UpdatedAtUtc = DateTime.UtcNow;
        Audit(db, who, http, id, archived ? "record.archived" : "record.restored");
        await db.SaveChangesAsync(ct); return Results.Ok(row);
    }
    private static async Task<IResult> AddChildAsync<T>(Guid id, T row, string action, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct) where T : RecordChild
    {
        var parent = await RecordAsync(db, id, ct, true);
        parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
        if (await db.Set<T>().CountAsync(x => x.RecordId == id, ct) >= 100) throw new ValidationException("This record has reached the limit of 100 items in this section.");
        row.RecordId = id; row.TenantId = who.TenantId!.Value; db.Add(row);
        Audit(db, who, http, id, action); await db.SaveChangesAsync(ct);
        return Results.Ok(new { row.Id });
    }
    private static async Task<IResult> DeleteChildAsync(Guid id, string collection, Guid childId, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var parent = await RecordAsync(db, id, ct, true);
        parent.Version = Guid.NewGuid(); parent.UpdatedAtUtc = DateTime.UtcNow;
        RecordChild? row = collection switch
        {
            "addresses" => await db.Set<Address>().SingleOrDefaultAsync(x => x.Id == childId && x.RecordId == id, ct),
            "communications" => await db.Set<CommunicationMethod>().SingleOrDefaultAsync(x => x.Id == childId && x.RecordId == id, ct),
            "relationships" => await db.Set<RecordRelationship>().SingleOrDefaultAsync(x => x.Id == childId && x.RecordId == id, ct),
            "tags" => await db.Set<RecordTag>().SingleOrDefaultAsync(x => x.Id == childId && x.RecordId == id, ct),
            _ => null
        };
        if (row == null) throw new KeyNotFoundException();
        db.Remove(row); Audit(db, who, http, id, collection + ".removed"); await db.SaveChangesAsync(ct); return Results.NoContent();
    }
    private static async Task<IResult> UploadAsync(Guid id, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        await RecordAsync(db, id, ct, true);
        if (http.Request.ContentLength is null or > 6 * 1024 * 1024) throw new ValidationException("Choose a file up to 5 MB.");
        if (!http.Request.HasFormContentType) throw new ValidationException("Upload a multipart form containing a file.");
        var form = await http.Request.ReadFormAsync(ct); var file = form.Files.GetFile("file");
        if (file == null || file.Length is <= 0 or > 5 * 1024 * 1024) throw new ValidationException("Choose a file up to 5 MB.");
        var name = Required(Path.GetFileName(file.FileName.Replace('\\', '/')), 160);
        if (name.Any(char.IsControl)) throw new ValidationException("Use a filename without control characters.");
        if (!new[] { ".pdf", ".txt", ".csv", ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(name).ToLowerInvariant())) throw new ValidationException("Supported files: PDF, text, CSV, PNG and JPEG.");
        using var buffer = new MemoryStream(); await file.CopyToAsync(buffer, ct);
        return await AddChildAsync(id, new RecordFile { Name = name, Content = buffer.ToArray() }, "file.added", db, who, http, ct);
    }
    private static async Task<IResult> SetFieldAsync(Guid id, Guid fieldId, ValueRequest r, CrmDbContext db, IRequestIdentity who, HttpContext http, CancellationToken ct)
    {
        var record = await RecordAsync(db, id, ct, true);
        var definition = await db.Set<CustomFieldDefinition>().SingleOrDefaultAsync(x => x.Id == fieldId && x.Kind == record.Kind, ct) ?? throw new KeyNotFoundException();
        if ((r.TextValue != null && definition.DataType != "text") || (r.NumberValue != null && definition.DataType != "number") || (r.BooleanValue != null && definition.DataType != "boolean") || (r.DateValue != null && definition.DataType != "date")) throw new ValidationException("The value does not match the field type.");
        if (r.NumberValue is decimal n && ((n >= 100000000000000m || n <= -100000000000000m) || decimal.Round(n, 4) != n)) throw new ValidationException("Use at most 14 integer digits and 4 decimal places.");
        var row = await db.Set<CustomFieldValue>().SingleOrDefaultAsync(x => x.RecordId == id && x.DefinitionId == fieldId, ct);
        if (row == null) { row = new CustomFieldValue { TenantId = who.TenantId!.Value, RecordId = id, DefinitionId = fieldId }; db.Add(row); }
        record.Version = Guid.NewGuid(); record.UpdatedAtUtc = DateTime.UtcNow;
        row.TextValue = Optional(r.TextValue, 500); row.NumberValue = r.NumberValue; row.BooleanValue = r.BooleanValue; row.DateValue = r.DateValue;
        Audit(db, who, http, id, "custom-field.updated"); await db.SaveChangesAsync(ct); return Results.Ok(row);
    }
    private static async Task ValidateAsync(RecordRequest r, IIdentityDirectory directory, CancellationToken ct)
    {
        Choice(r.Kind, "contact", "account"); Choice(r.Status, "active", "prospect", "inactive"); Required(r.Name, 160); Optional(r.Category, 100);
        if (!await directory.IsValidOwnerAsync(r.OwnerUserId, r.OwnerTeamId, ct)) throw new ValidationException("Choose an active owner and team from your tenant.");
    }
    private static void Apply(CrmRecord row, RecordRequest r)
    {
        row.Name = r.Name.Trim(); row.Status = r.Status; row.Category = Optional(r.Category, 100); row.OwnerUserId = r.OwnerUserId; row.OwnerTeamId = r.OwnerTeamId;
        row.Version = Guid.NewGuid(); row.UpdatedAtUtc = DateTime.UtcNow;
    }
    private static async Task<CrmRecord> RecordAsync(CrmDbContext db, Guid id, CancellationToken ct, bool active = false)
    {
        var row = await db.Records.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        if (active && row.IsArchived) throw new ValidationException("Restore this archived record before changing it.");
        return row;
    }
    private static void Audit(CrmDbContext db, IRequestIdentity who, HttpContext http, Guid id, string action) => db.Add(new Activity
    { TenantId = who.TenantId!.Value, RecordId = id, ActorUserId = who.UserId!.Value, Action = action, CorrelationId = http.TraceIdentifier });
    private static IResult Conflict() => Results.Problem(statusCode: 409, title: "This record changed. Reload it before saving again.");
    private static void Choice(string? value, params string[] allowed) { if (value == null || !allowed.Contains(value)) throw new ValidationException("Unsupported choice: " + value); }
    private static string Required(string? value, int max) => Optional(value, max) ?? throw new ValidationException("A required field is empty.");
    private static string? Optional(string? value, int max)
    {
        if (value?.Length > max) throw new ValidationException($"Use at most {max} characters.");
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
