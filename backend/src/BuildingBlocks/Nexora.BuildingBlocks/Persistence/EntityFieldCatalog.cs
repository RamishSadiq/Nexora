using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nexora.BuildingBlocks.Persistence;

/// <summary>Apply to a scalar that must never be offered to rules or workflow expressions.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AutomationIgnoreAttribute : Attribute;

public static class EntityFieldCatalog
{
    public static string? DataType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) || type.IsEnum) return "text";
        if (type == typeof(Guid)) return "reference";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime) || type == typeof(DateOnly) || type == typeof(DateTimeOffset)) return "date";
        return type == typeof(decimal) || type == typeof(int) || type == typeof(long) || type == typeof(short)
            || type == typeof(byte) || type == typeof(double) || type == typeof(float) ? "number" : null;
    }

    public static PropertyInfo[] Properties(Type type) => type.GetProperties()
        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.Name is not ("TenantId" or "Version")
            && p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() == null
            && p.GetCustomAttribute<AutomationIgnoreAttribute>() == null && DataType(p.PropertyType) != null).ToArray();

    public static string Label(string name) => Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");

    // Rules store numeric values as decimal and dates as UTC DateTime, regardless of CLR storage type.
    public static object? RuleValue(object? value) => value switch
    {
        null => null,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        DateTimeOffset date => date.UtcDateTime,
        Guid id => id.ToString(),
        Enum item => item.ToString(),
        byte or short or int or long or float or double => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
        _ => value
    };
}

public interface IAutomationRecordSources
{
    IEnumerable<Guid> ChangedRecordIds(string kind, object[] changed);
    Task<object?> ReadSourceAsync(object record, string source, CancellationToken ct);
}

public interface IAutomationFieldProvider
{
    Task<AutomationField[]> AutomationFieldsAsync(string kind, CancellationToken ct);
}
