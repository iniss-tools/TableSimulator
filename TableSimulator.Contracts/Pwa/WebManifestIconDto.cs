using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Pwa;

/// <summary>Ikona aplikacie v manifeste.</summary>
/// <param name="Src">adresa ikony</param>
/// <param name="Sizes">rozmery (napr. 192x192, any)</param>
/// <param name="Type">typ suboru</param>
/// <param name="Purpose">any alebo maskable (orezanie podla systemu)</param>
public sealed record WebManifestIconDto(
    [property: JsonPropertyName("src")] string Src,
    [property: JsonPropertyName("sizes")] string Sizes,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("purpose")] string Purpose);
