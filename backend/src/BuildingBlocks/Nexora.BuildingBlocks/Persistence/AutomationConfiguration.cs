using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Nexora.BuildingBlocks.Security;

namespace Nexora.BuildingBlocks.Persistence;

public sealed class AutomationConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string EntityType { get; set; } = "";
    public string SectionsJson { get; set; } = "[]";
    public string RulesJson { get; set; } = "[]";
    public Guid Version { get; set; } = Guid.NewGuid();
}

public interface IAutomationValue
{
    Guid TenantId { get; set; }
    Guid RecordId { get; set; }
    Guid DefinitionId { get; set; }
    string? TextValue { get; set; }
    decimal? NumberValue { get; set; }
    bool? BooleanValue { get; set; }
    DateTime? DateValue { get; set; }
}

public sealed record AutomationField(string Id, string Name, string DataType, bool Writable);
public sealed record AutomationRule(string Source, string Operation, string Target, string? Operand = null, string? Value = null);
public sealed record AutomationRequest(Guid? Version, string[]? Sections, AutomationRule[]? Rules);

/// <summary>Bounded, typed rules. Targets are custom attributes, so automations cannot bypass domain transitions.</summary>
public static class Automations
{
    public static void Configure(ModelBuilder b)
    {
        var row = b.Entity<AutomationConfiguration>();
        row.ToTable("AutomationConfigurations").HasKey(x => x.Id);
        row.HasIndex(x => new { x.TenantId, x.EntityType }).IsUnique();
        row.Property(x => x.EntityType).HasMaxLength(80);
        row.Property(x => x.RulesJson).HasMaxLength(100000);
        row.Property(x => x.SectionsJson).HasMaxLength(4000);
        row.Property(x => x.Version).IsConcurrencyToken();
    }

    public static string[] Sections(string kind) => kind is "contact" or "account"
        ? ["Addresses", "Communication methods", "Related records", "Notes", "Tags", "Files", "Custom fields", "Activity timeline"]
        : ["Record selection", "Custom fields"];

    public static AutomationField[] BuiltIns(Type type) => type.GetProperties()
        .Where(p => new[] { "Name", "Title", "Status", "Category", "CreatedAtUtc", "StartsAtUtc", "EndsAtUtc", "DateOfBirth", "Priority" }.Contains(p.Name))
        .Select(p => new AutomationField("record:" + p.Name, p.Name, (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) == typeof(DateTime) ? "date" : "text", false)).ToArray();

    public static void Map<TDb>(RouteGroupBuilder api, string permission, Func<string, Type?> resolve,
        Func<TDb, string, CancellationToken, Task<AutomationField[]>> fields) where TDb : DbContext
    {
        api.MapGet("/automations/{entityType}", async (string entityType, TDb db, IRequestIdentity who, HttpContext http, CancellationToken ct) =>
        {
            var type = resolve(entityType) ?? throw new KeyNotFoundException();
            var row = await db.Set<AutomationConfiguration>().SingleOrDefaultAsync(x => x.EntityType == entityType && x.TenantId == who.TenantId, ct);
            return Results.Ok(new { version = row?.Version, sections = row == null ? Sections(entityType) : JsonSerializer.Deserialize<string[]>(row.SectionsJson),
                rules = row == null ? [] : ReadRules(row), canManage = http.User.HasClaim("permission", permission), availableSections = Sections(entityType), fields = BuiltIns(type).Concat(await fields(db, entityType, ct)) });
        });
        api.MapPut("/automations/{entityType}", async (string entityType, AutomationRequest request, TDb db, IRequestIdentity who, CancellationToken ct) =>
        {
            var type = resolve(entityType) ?? throw new KeyNotFoundException();
            var row = await db.Set<AutomationConfiguration>().SingleOrDefaultAsync(x => x.EntityType == entityType && x.TenantId == who.TenantId, ct);
            if (request.Version != row?.Version) return Results.Problem(statusCode: 409, title: "Settings changed. Reload before saving.");
            if (request.Sections == null && request.Rules == null) throw new ValidationException("Provide a layout or rules.");
            if (request.Sections is {} sections && (sections.Length != Sections(entityType).Length || sections.Distinct().Count() != sections.Length || Sections(entityType).Except(sections).Any()))
                throw new ValidationException("Include each available section exactly once.");
            if (request.Rules is {} rules) Validate(rules, BuiltIns(type).Concat(await fields(db, entityType, ct)).ToArray());
            if (row == null) { row = new() { TenantId = who.TenantId!.Value, EntityType = entityType, SectionsJson = JsonSerializer.Serialize(Sections(entityType)) }; db.Add(row); }
            if (request.Sections != null) row.SectionsJson = JsonSerializer.Serialize(request.Sections);
            if (request.Rules != null) row.RulesJson = JsonSerializer.Serialize(request.Rules);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Version });
        }).RequireAuthorization(permission);
    }

    public static AutomationRule[] ReadRules(AutomationConfiguration row) => JsonSerializer.Deserialize<AutomationRule[]>(row.RulesJson) ?? [];

    public static AutomationRule[] Validate(AutomationRule[] rules, AutomationField[] fields)
    {
        if (rules.Length > 50 || rules.Any(r => r == null)) throw new ValidationException("Provide at most 50 rules.");
        var targets = new HashSet<string>();
        foreach (var rule in rules)
        {
            var source = fields.SingleOrDefault(f => f.Id == rule.Source);
            var target = fields.SingleOrDefault(f => f.Id == rule.Target && f.Writable);
            if (source == null || target == null) throw new ValidationException("A rule references an unavailable attribute. Remove the rule before deleting its attribute.");
            if (!targets.Add(rule.Target)) throw new ValidationException("Only one rule may write each target attribute.");
            var valid = rule.Operation switch
            {
                "copy" => source.DataType == target.DataType,
                "age" => source.DataType == "date" && target.DataType == "number",
                "add" or "subtract" => source.DataType == "number" && target.DataType == "number" && decimal.TryParse(rule.Operand, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
                "equals" or "not-equal" => target.DataType == "boolean" && ValidOperand(rule.Operand, source.DataType),
                "contains" => source.DataType == "text" && target.DataType == "boolean" && !string.IsNullOrEmpty(rule.Operand),
                "set-when-equal" => source.DataType == "text" && target.DataType == "text" && rule.Operand != null && rule.Value != null,
                _ => false
            };
            if (!valid || rule.Operand?.Length > 500 || rule.Value?.Length > 500) throw new ValidationException("Choose a compatible function, target type and operand.");
        }
        var ordered = new List<AutomationRule>();
        var remaining = rules.ToList();
        while (remaining.Count > 0)
        {
            var ready = remaining.Where(r => remaining.All(other => other.Target != r.Source)).ToArray();
            if (ready.Length == 0) throw new ValidationException("Rules cannot contain circular dependencies.");
            ordered.AddRange(ready); foreach (var rule in ready) remaining.Remove(rule);
        }
        return ordered.ToArray();
    }

    private static bool ValidOperand(string? input, string type) => input != null && (type switch
    {
        "number" => decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
        "boolean" => bool.TryParse(input, out _),
        "date" => DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
        _ => true
    });

    public static async Task GuardAsync(DbContext db, Guid? tenant, Guid? user, CancellationToken ct)
    {
        foreach (var entry in db.ChangeTracker.Entries<AutomationConfiguration>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (tenant == null || user == null || entry.Entity.TenantId != tenant) throw new TenantBoundaryException();
            if (entry.State != EntityState.Added)
            {
                var stored = await entry.GetDatabaseValuesAsync(ct);
                if (stored == null || !Equals(stored[nameof(AutomationConfiguration.TenantId)], tenant) || !Equals(stored[nameof(AutomationConfiguration.EntityType)], entry.Entity.EntityType)) throw new TenantBoundaryException();
                entry.Entity.Version = Guid.NewGuid();
            }
        }
    }

    public static async Task ApplyAsync<TValue>(DbContext db, Guid? tenant, Func<string, Type?> resolve,
        Func<object, string> kindOf, Func<string, CancellationToken, Task<AutomationField[]>> fields, CancellationToken ct) where TValue : class, IAutomationValue, new()
    {
        if (tenant == null) return;
        db.ChangeTracker.DetectChanges();
        var changed = db.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified).Select(e => e.Entity).ToArray();
        var configurations = await db.Set<AutomationConfiguration>().Where(x => x.TenantId == tenant).ToListAsync(ct);
        foreach (var configuration in configurations)
        {
            var rules = ReadRules(configuration);
            if (rules.Length == 0) continue;
            var type = resolve(configuration.EntityType) ?? throw new ValidationException("Unknown automation entity.");
            var metadata = BuiltIns(type).Concat(await fields(configuration.EntityType, ct)).ToArray();
            rules = Validate(rules, metadata);
            var ids = changed.OfType<TValue>().Select(x => x.RecordId)
                .Concat(changed.Where(x => x.GetType() == type && kindOf(x) == configuration.EntityType).Select(x => (Guid)type.GetProperty("Id")!.GetValue(x)!)).Distinct().ToArray();
            foreach (var id in ids)
            {
                var record = await db.FindAsync(type, [id], ct);
                if (record == null || db.Entry(record).State == EntityState.Deleted || kindOf(record) != configuration.EntityType || !Equals(type.GetProperty("TenantId")!.GetValue(record), tenant)) continue;
                await db.Set<TValue>().Where(x => x.RecordId == id && x.TenantId == tenant).LoadAsync(ct);
                var values = db.ChangeTracker.Entries<TValue>().Where(e => e.State != EntityState.Deleted && e.Entity.RecordId == id && e.Entity.TenantId == tenant).Select(e => e.Entity).ToDictionary(x => x.DefinitionId);
                foreach (var rule in rules)
                {
                    var sourceField = metadata.Single(f => f.Id == rule.Source);
                    object? source = rule.Source.StartsWith("record:", StringComparison.Ordinal)
                        ? type.GetProperty(rule.Source[7..])!.GetValue(record)
                        : values.TryGetValue(Guid.Parse(rule.Source), out var input) ? ReadValue(input, sourceField.DataType) : null;
                    var result = Evaluate(rule, source, sourceField.DataType, DateTime.UtcNow.Date);
                    var targetId = Guid.Parse(rule.Target);
                    if (!values.TryGetValue(targetId, out var target)) { target = new TValue { TenantId = tenant.Value, RecordId = id, DefinitionId = targetId }; db.Add(target); values.Add(targetId, target); }
                    target.TextValue = result as string; target.NumberValue = result as decimal?;
                    target.BooleanValue = result as bool?; target.DateValue = result as DateTime?;
                    if (target.TextValue?.Length > 500 || target.NumberValue is decimal n && (n >= 100000000000000m || n <= -100000000000000m || decimal.Round(n, 4) != n)) throw new ValidationException("A calculated value exceeds the attribute limits.");
                }
            }
        }
    }

    private static object? ReadValue(IAutomationValue value, string type) => type switch { "number" => value.NumberValue, "boolean" => value.BooleanValue, "date" => value.DateValue, _ => value.TextValue };

    public static object? Evaluate(AutomationRule rule, object? source, string type, DateTime today)
    {
        if (source == null) return null;
        try
        {
            var operand = rule.Operand;
            var equal = type switch
            {
                "number" => decimal.TryParse(operand, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) && (decimal)source == n,
                "boolean" => bool.TryParse(operand, out var b) && (bool)source == b,
                "date" => DateTime.TryParseExact(operand, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && ((DateTime)source).Date == d.Date,
                _ => string.Equals((string)source, operand, StringComparison.OrdinalIgnoreCase)
            };
            return rule.Operation switch
            {
                "copy" => source,
                "age" => ((DateTime)source).Date > today ? null : (decimal)(today.Year - ((DateTime)source).Year - (((DateTime)source).Date.AddYears(today.Year - ((DateTime)source).Year) > today ? 1 : 0)),
                "add" => checked((decimal)source + decimal.Parse(operand!, CultureInfo.InvariantCulture)),
                "subtract" => checked((decimal)source - decimal.Parse(operand!, CultureInfo.InvariantCulture)),
                "equals" => equal,
                "not-equal" => !equal,
                "contains" => ((string)source).Contains(operand!, StringComparison.OrdinalIgnoreCase),
                "set-when-equal" => equal ? rule.Value : null,
                _ => throw new ValidationException("Unsupported rule function.")
            };
        }
        catch (OverflowException) { throw new ValidationException("A calculated number exceeds the supported range."); }
    }
}
