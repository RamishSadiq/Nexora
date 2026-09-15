using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexora.BuildingBlocks.Domain;
using Nexora.BuildingBlocks.Security;

namespace Nexora.BuildingBlocks.Persistence;

public sealed class AttributeDefinition : TenantEntity
{
    public string EntityType { get; set; } = "";
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "text";
    public int DisplayOrder { get; set; }
}
public sealed class AttributeValue : TenantEntity
{
    public Guid RecordId { get; set; }
    public Guid DefinitionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateValue { get; set; }
}
public sealed record AttributeInput(string? TextValue, decimal? NumberValue, bool? BooleanValue, DateTime? DateValue);
public interface IAttributeRequest { Dictionary<Guid, AttributeInput>? Attributes { get; } }
public sealed record AttributeDefinitionRequest(string Name, string DataType);
public sealed record AttributeOrderRequest(Guid[] Ids);
public sealed record AttributeValuesRequest(Dictionary<Guid, AttributeInput> Attributes) : IAttributeRequest;

public static class EntityAttributes
{
    public static RouteGroupBuilder WithAttributes<TDb>(this RouteGroupBuilder api, string permission, params Type[] entities) where TDb : TenantDbContext
    {
        Type Resolve(string entityType) => entities.SingleOrDefault(t => t.Name == entityType) ?? throw new KeyNotFoundException();
        api.MapGet("/attribute-entities", () => entities.Select(t => t.Name));
        api.MapGet("/attributes/{entityType}/records", async (string entityType, int? page, TDb db, CancellationToken ct) =>
        {
            var method = typeof(EntityAttributes).GetMethod(nameof(ListRecords), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.MakeGenericMethod(Resolve(entityType));
            return await (Task<object>)method.Invoke(null, [db, Math.Max(1, page ?? 1), ct])!;
        });
        api.MapGet("/attributes/{entityType}/definitions", async (string entityType, TDb db, CancellationToken ct) =>
        {
            Resolve(entityType);
            return await Definitions(db, entityType).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        });
        api.MapPost("/attributes/{entityType}/definitions", async (string entityType, AttributeDefinitionRequest request, TDb db, IRequestIdentity who, CancellationToken ct) =>
        {
            Resolve(entityType); ValidateDefinition(request);
            if (await Definitions(db, entityType).CountAsync(ct) >= 50) throw new ValidationException("An entity may have at most 50 attributes.");
            var row = new AttributeDefinition { TenantId = who.TenantId!.Value, EntityType = entityType, Name = request.Name.Trim(), DataType = request.DataType,
                DisplayOrder = (await Definitions(db, entityType).MaxAsync(x => (int?)x.DisplayOrder, ct) ?? -1) + 1 };
            db.Add(row); await db.SaveChangesAsync(ct); return Results.Ok(row);
        }).RequireAuthorization(permission);
        api.MapPatch("/attributes/{entityType}/definitions/{id:guid}", async (string entityType, Guid id, AttributeDefinitionRequest request, TDb db, CancellationToken ct) =>
        {
            Resolve(entityType); ValidateDefinition(request);
            var row = await Definitions(db, entityType).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            if (row.DataType != request.DataType && await db.Set<AttributeValue>().AnyAsync(x => x.DefinitionId == id, ct))
                throw new ValidationException("Cannot change data type while saved values exist.");
            row.Name = request.Name.Trim(); row.DataType = request.DataType;
            await db.SaveChangesAsync(ct); return Results.Ok(row);
        }).RequireAuthorization(permission);
        api.MapDelete("/attributes/{entityType}/definitions/{id:guid}", async (string entityType, Guid id, TDb db, CancellationToken ct) =>
        {
            Resolve(entityType);
            var row = await Definitions(db, entityType).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
            db.RemoveRange(await db.Set<AttributeValue>().Where(x => x.DefinitionId == id).ToListAsync(ct));
            db.Remove(row); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization(permission);
        api.MapPut("/attributes/{entityType}/order", async (string entityType, AttributeOrderRequest request, TDb db, CancellationToken ct) =>
        {
            Resolve(entityType); var rows = await Definitions(db, entityType).ToListAsync(ct);
            if (request.Ids == null || request.Ids.Length != rows.Count || request.Ids.Distinct().Count() != rows.Count || rows.Any(x => !request.Ids.Contains(x.Id)))
                throw new ValidationException("Reload and include every attribute exactly once.");
            foreach (var row in rows) row.DisplayOrder = Array.IndexOf(request.Ids, row.Id);
            await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization(permission);
        api.MapGet("/attributes/{entityType}/records/{id:guid}", async (string entityType, Guid id, TDb db, IRequestIdentity who, CancellationToken ct) =>
        {
            await FindRecord(db, Resolve(entityType), id, who, ct);
            return await (from value in db.Set<AttributeValue>() join definition in Definitions(db, entityType) on value.DefinitionId equals definition.Id where value.RecordId == id select value).ToListAsync(ct);
        });
        api.MapPut("/attributes/{entityType}/records/{id:guid}", async (string entityType, Guid id, AttributeValuesRequest request, TDb db, IRequestIdentity who, CancellationToken ct) =>
        {
            var record = await FindRecord(db, Resolve(entityType), id, who, ct);
            await Apply(db, record, request.Attributes, ct); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization(permission);
        // Attribute requests share the module's transaction with the entity write.
        // No persisted parent remains if its typed values fail validation.
        api.AddEndpointFilter(async (context, next) =>
        {
            var request = context.Arguments.OfType<IAttributeRequest>().FirstOrDefault();
            if (request?.Attributes == null || context.HttpContext.Request.Path.Value!.Contains("/attributes/")) return await next(context);
            var db = context.HttpContext.RequestServices.GetRequiredService<TDb>();
            await using var transaction = await db.Database.BeginTransactionAsync(context.HttpContext.RequestAborted);
            var result = await next(context);
            if (result is IStatusCodeHttpResult status && status.StatusCode >= 400) return result;
            if (result is not IValueHttpResult { Value: TenantEntity record } || !entities.Contains(record.GetType()))
                throw new ValidationException("This operation does not support attribute values.");
            await Apply(db, record, request.Attributes, context.HttpContext.RequestAborted);
            await db.SaveChangesAsync(context.HttpContext.RequestAborted);
            await transaction.CommitAsync(context.HttpContext.RequestAborted);
            return result;
        });
        return api;
    }
    private static IQueryable<AttributeDefinition> Definitions(TenantDbContext db, string type) => db.Set<AttributeDefinition>().Where(x => x.EntityType == type);
    private static async Task<object> ListRecords<T>(TenantDbContext db, int page, CancellationToken ct) where T : TenantEntity
    {
        var rows = await db.Set<T>().AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id).Skip((Math.Min(page, 10000) - 1) * 50).Take(50).ToListAsync(ct);
        return rows.Select(row => new { row.Id, name = new[] { "Name", "Title", "ContactName", "CustomerName", "Subject", "Reference", "Number" }.Select(p => typeof(T).GetProperty(p)?.GetValue(row)?.ToString()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? $"{typeof(T).Name} · {row.CreatedAtUtc:yyyy-MM-dd} · {row.Id.ToString()[..8]}" }).ToList();
    }
    private static async Task<TenantEntity> FindRecord(TenantDbContext db, Type type, Guid id, IRequestIdentity who, CancellationToken ct)
    {
        var row = await db.FindAsync(type, [id], ct) as TenantEntity;
        if (row == null || row.TenantId != who.TenantId) throw new KeyNotFoundException();
        return row;
    }
    private static void ValidateDefinition(AttributeDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 80) throw new ValidationException("Enter an attribute name of 1–80 characters.");
        if (!new[] { "text", "number", "boolean", "date" }.Contains(request.DataType)) throw new ValidationException("Unsupported data type.");
    }
    private static async Task Apply(TenantDbContext db, TenantEntity record, Dictionary<Guid, AttributeInput>? values, CancellationToken ct)
    {
        if (values == null || values.Count > 50) throw new ValidationException("Provide at most 50 attribute values.");
        var definitions = await Definitions(db, record.GetType().Name).ToListAsync(ct);
        var existing = await db.Set<AttributeValue>().Where(x => x.RecordId == record.Id).ToListAsync(ct);
        foreach (var (id, input) in values)
        {
            var definition = definitions.SingleOrDefault(x => x.Id == id) ?? throw new ValidationException("An attribute was deleted or is unavailable. Reload the form.");
            if (input == null || input.TextValue?.Length > 500 || (input.TextValue != null && definition.DataType != "text") || (input.NumberValue != null && definition.DataType != "number") || (input.BooleanValue != null && definition.DataType != "boolean") || (input.DateValue != null && definition.DataType != "date")) throw new ValidationException("The value does not match the attribute data type.");
            if (input.NumberValue is decimal n && (n >= 100000000000000m || n <= -100000000000000m || decimal.Round(n, 4) != n)) throw new ValidationException("Use up to 14 integer digits and 4 decimal places.");
            var row = existing.SingleOrDefault(x => x.DefinitionId == id);
            if (row == null) { row = new AttributeValue { TenantId = record.TenantId, RecordId = record.Id, DefinitionId = id }; db.Add(row); }
            row.TextValue = input.TextValue; row.NumberValue = input.NumberValue; row.BooleanValue = input.BooleanValue; row.DateValue = input.DateValue;
        }
    }
}
