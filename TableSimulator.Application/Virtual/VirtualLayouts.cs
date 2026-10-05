using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

/// <summary>
/// Vstavane rozlozenia virtualnych tabul (subory <c>Virtual/Layouts/*.json</c> vlozene do zostavy) a prevod
/// vlastnych rozlozeni z databazy.
/// </summary>
public static class VirtualLayouts
{
    /// <summary>Rozlozenie tabul, ktore ziadne nemaju (alebo ich rozlozenie uz neexistuje).</summary>
    public const string DefaultId = "modra";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Lazy<VirtualLayoutDto[]> Loaded = new(Load);

    /// <summary>Vstavane rozlozenia v poradi pre obsluhu (predvolene prve).</summary>
    public static IReadOnlyList<VirtualLayoutDto> BuiltIn => Loaded.Value;

    /// <summary>Predvolene rozlozenie.</summary>
    public static VirtualLayoutDto Default => BuiltIn.First(l => l.Id == DefaultId);

    /// <summary>Vstavane rozlozenie s klucom; null, ak take nie je.</summary>
    public static VirtualLayoutDto? FindBuiltIn(string id) => BuiltIn.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Rozlozenie ako JSON do databazy.</summary>
    public static string Serialize(VirtualLayoutDto layout) => JsonSerializer.Serialize(layout with { BuiltIn = false }, Json);

    /// <summary>Rozlozenie z databazy; kluc a nazov z riadku databazy.</summary>
    public static VirtualLayoutDto Deserialize(VirtualLayoutRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var layout = JsonSerializer.Deserialize<VirtualLayoutDto>(record.Definition, Json) ?? new VirtualLayoutDto();
        return layout with { Id = record.Id, Name = record.Name, BuiltIn = false };
    }

    /// <summary>
    /// Rozlozenie podla mena sablony XSL INISSView alebo kluca tabule (<c>Pokladny_2r</c>, <c>Odjezdy_…_cerna</c>,
    /// <c>Odchody web</c>); prazdne = predvolene.
    /// </summary>
    public static string Guess(string text)
    {
        var lower = WithoutDiacritics(text ?? "").ToLowerInvariant();
        if (lower.Contains("pokladn", StringComparison.Ordinal))
            return "pokladne";
        if (lower.Contains("cern", StringComparison.Ordinal))
            return "cierna";
        if (lower.Contains("odj", StringComparison.Ordinal) || lower.Contains("odch", StringComparison.Ordinal))
            return "odchody";
        if (lower.Contains("prij", StringComparison.Ordinal) || lower.Contains("prj", StringComparison.Ordinal) || lower.Contains("pric", StringComparison.Ordinal))
            return "prichody";
        return "";
    }

    // Príchody, Příjezdy -> Prichody, Prijezdy
    private static string WithoutDiacritics(string text) =>
        new(text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());

    private static VirtualLayoutDto[] Load()
    {
        var assembly = typeof(VirtualLayouts).Assembly;
        var layouts = assembly.GetManifestResourceNames()
            .Where(n => n.Contains(".Virtual.Layouts.", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
            .Select(n => Read(assembly, n))
            .ToList();
        // predvolene prve, ostatne podla nazvu
        return layouts.OrderBy(l => l.Id != DefaultId).ThenBy(l => l.Name, StringComparer.CurrentCulture).ToArray();
    }

    private static VirtualLayoutDto Read(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)!;
        var layout = JsonSerializer.Deserialize<VirtualLayoutDto>(stream, Json)
                     ?? throw new InvalidOperationException($"Built-in layout {resource} is empty.");
        return layout with { BuiltIn = true };
    }
}
