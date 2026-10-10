using System.Text.Json;

namespace Nexora.BuildingBlocks.Persistence;

public sealed record AutomationSection(string Id, string Title, string Region, bool AcceptsFields = false);

public static class AutomationCatalog
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private static readonly Dictionary<string, AutomationSection[]> Definitions = Load();

    private static Dictionary<string, AutomationSection[]> Load()
    {
        using var stream = typeof(AutomationCatalog).Assembly.GetManifestResourceStream("Nexora.AutomationCatalog.json")
            ?? throw new InvalidOperationException("Automation catalogue is missing.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("sections").Deserialize<Dictionary<string, AutomationSection[]>>(Json)!;
    }

    public static AutomationSection[] Sections(string kind) => Definitions.GetValueOrDefault(kind) ?? Definitions["default"];

    public static string FieldSection(string kind, string? section)
    {
        section = string.IsNullOrWhiteSpace(section) ? "General Information" : section.Trim();
        if (!Sections(kind).Any(candidate => candidate.AcceptsFields && candidate.Id == section))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Choose an available section for this field.");
        return section;
    }

    public static string[] Reconcile(IEnumerable<string>? saved, string[] available) =>
        (saved ?? []).Where(available.Contains).Distinct().Concat(available).Distinct().ToArray();
}
