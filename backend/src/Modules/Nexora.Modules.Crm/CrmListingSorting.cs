using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nexora.Modules.Crm.Domain;
using Nexora.Modules.Crm.Infrastructure;

namespace Nexora.Modules.Crm;

internal static class CrmListingSorting
{
    private static string Canonical(string sort) => string.IsNullOrWhiteSpace(sort) ? throw new ValidationException("Invalid sorting column.") : sort == "updated" ? "-updatedAtUtc" : sort;
    private static string Key(string sort) => sort.StartsWith('-') ? sort[1..] : sort;

    public static async Task ValidateAsync(CrmDbContext db, string kind, string sort, CancellationToken ct)
        => _ = await DefinitionAsync(db, kind, Key(Canonical(sort)), ct);

    private static async Task<CustomFieldDefinition?> DefinitionAsync(CrmDbContext db, string kind, string key, CancellationToken ct)
    {
        if (key is "name" or "status" or "category" or "updatedAtUtc") return null;
        if (key.StartsWith("field:") && Guid.TryParse(key[6..], out var id))
            return await db.Set<CustomFieldDefinition>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Kind == kind, ct)
                ?? throw new ValidationException("The sorting field is unavailable for this record type.");
        throw new ValidationException("Invalid sorting column.");
    }

    private static IOrderedQueryable<CrmRecord> Order<T>(IQueryable<CrmRecord> query, Expression<Func<CrmRecord, T>> value, bool descending)
        => descending ? query.OrderByDescending(value) : query.OrderBy(value);

    public static async Task<IOrderedQueryable<CrmRecord>> ApplyAsync(IQueryable<CrmRecord> query, CrmDbContext db, string kind, string sort, CancellationToken ct)
    {
        sort = Canonical(sort);
        var descending = sort.StartsWith('-');
        var key = Key(sort);
        var field = await DefinitionAsync(db, kind, key, ct);
        if (field == null) return key switch
        {
            "status" => Order(query, x => x.Status, descending),
            "category" => Order(query, x => x.Category, descending),
            "updatedAtUtc" => Order(query, x => x.UpdatedAtUtc, descending),
            _ => Order(query, x => x.Name, descending)
        };
        var values = db.Set<CustomFieldValue>();
        return field.DataType switch
        {
            "number" => Order(query, x => values.Where(v => v.RecordId == x.Id && v.DefinitionId == field.Id).Select(v => v.NumberValue).FirstOrDefault(), descending),
            "date" => Order(query, x => values.Where(v => v.RecordId == x.Id && v.DefinitionId == field.Id).Select(v => v.DateValue).FirstOrDefault(), descending),
            "boolean" => Order(query, x => values.Where(v => v.RecordId == x.Id && v.DefinitionId == field.Id).Select(v => v.BooleanValue).FirstOrDefault(), descending),
            _ => Order(query, x => values.Where(v => v.RecordId == x.Id && v.DefinitionId == field.Id).Select(v => v.TextValue).FirstOrDefault(), descending)
        };
    }
}
