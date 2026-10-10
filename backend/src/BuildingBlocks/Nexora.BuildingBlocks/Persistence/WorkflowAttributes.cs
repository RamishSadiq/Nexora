using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Nexora.BuildingBlocks.Persistence;

internal interface IWorkflowAttributes
{
    WorkflowField[] Fields { get; }
    Expression? ValueExpression(string key, ParameterExpression record);
    Task<WorkflowRecord[]> EnrichAsync(WorkflowRecord[] records, CancellationToken ct);
    void Snapshot(Guid id, Dictionary<string, string?> current, Dictionary<string, string?> previous, bool created);
}

internal static class WorkflowAttributes
{
    public static IWorkflowAttributes? Create(DbContext db, WorkflowEntityRegistration registration)
    {
        if (db is not IAutomationFieldProvider provider) return null;
        var valueType = db.Model.GetEntityTypes().Select(entity => entity.ClrType).FirstOrDefault(type => typeof(IAutomationValue).IsAssignableFrom(type));
        if (valueType == null) return null;
        var fields = provider.AutomationFieldsAsync(registration.DiscriminatorValue ?? registration.RecordType.Name, CancellationToken.None).GetAwaiter().GetResult();
        return (IWorkflowAttributes)Activator.CreateInstance(typeof(WorkflowAttributes<>).MakeGenericType(valueType), db, fields)!;
    }
}

internal sealed class WorkflowAttributes<TValue>(DbContext db, AutomationField[] definitions) : IWorkflowAttributes where TValue : class, IAutomationValue
{
    private static string Key(Guid id) => "attribute_" + id.ToString("N");
    private readonly Dictionary<string, AutomationField> fields = definitions.Where(f => Guid.TryParse(f.Id, out _))
        .ToDictionary(f => Key(Guid.Parse(f.Id)));
    public WorkflowField[] Fields => fields.Select(pair => new WorkflowField(pair.Key, pair.Value.Name, pair.Value.DataType)).ToArray();

    public void Snapshot(Guid id, Dictionary<string, string?> current, Dictionary<string, string?> previous, bool created)
    {
        if (fields.Count == 0) return;
        db.Set<TValue>().Where(value => value.RecordId == id).Load();
        foreach (var key in fields.Keys) { current[key] = null; if (!created) previous[key] = null; }
        foreach (var entry in db.ChangeTracker.Entries<TValue>().Where(entry => entry.Entity.RecordId == id && entry.Entity.TenantId == (db as Nexora.BuildingBlocks.Persistence.IWorkflowWriteIdentity)?.WorkflowIdentity.TenantId))
        {
            var key = Key(entry.Entity.DefinitionId);
            if (!fields.TryGetValue(key, out var field)) continue;
            var property = ValueProperty(field.DataType);
            current[key] = entry.State == EntityState.Deleted ? null : WorkflowEntityRegistration.Format(entry.Property(property).CurrentValue);
            if (!created) previous[key] = entry.State == EntityState.Added ? null : WorkflowEntityRegistration.Format(entry.Property(property).OriginalValue);
        }
    }

    private static string ValueProperty(string type) => type switch
    {
        "number" => nameof(IAutomationValue.NumberValue), "boolean" => nameof(IAutomationValue.BooleanValue),
        "date" => nameof(IAutomationValue.DateValue), _ => nameof(IAutomationValue.TextValue)
    };

    public Expression? ValueExpression(string key, ParameterExpression record)
    {
        if (!fields.TryGetValue(key, out var field)) return null;
        var value = Expression.Parameter(typeof(TValue), "attribute");
        var matches = Expression.AndAlso(
            Expression.Equal(Expression.Property(value, nameof(IAutomationValue.RecordId)), Expression.Property(record, "Id")),
            Expression.Equal(Expression.Property(value, nameof(IAutomationValue.DefinitionId)), Expression.Constant(Guid.Parse(field.Id))));
        var query = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [typeof(TValue)],
            ((IQueryable<TValue>)db.Set<TValue>()).Expression, Expression.Quote(Expression.Lambda(matches, value)));
        var member = Expression.Property(value, ValueProperty(field.DataType));
        var select = Expression.Call(typeof(Queryable), nameof(Queryable.Select), [typeof(TValue), member.Type],
            query, Expression.Quote(Expression.Lambda(member, value)));
        return Expression.Call(typeof(Queryable), nameof(Queryable.FirstOrDefault), [member.Type], select);
    }

    public async Task<WorkflowRecord[]> EnrichAsync(WorkflowRecord[] records, CancellationToken ct)
    {
        if (records.Length == 0 || fields.Count == 0) return records;
        var ids = records.Select(record => record.Id).ToArray();
        var definitions = fields.Values.Select(field => Guid.Parse(field.Id)).ToArray();
        var values = await db.Set<TValue>().AsNoTracking().Where(value => ids.Contains(value.RecordId) && definitions.Contains(value.DefinitionId)).ToArrayAsync(ct);
        var lookup = values.ToLookup(value => value.RecordId);
        foreach (var record in records)
        {
            foreach (var field in fields.Keys) record.Values[field] = null;
            foreach (var value in lookup[record.Id])
            {
                var key = Key(value.DefinitionId);
                object? data = fields[key].DataType switch
                {
                    "number" => value.NumberValue, "boolean" => value.BooleanValue,
                    "date" => value.DateValue, _ => value.TextValue
                };
                record.Values[key] = WorkflowEntityRegistration.Format(data);
            }
        }
        return records;
    }
}
