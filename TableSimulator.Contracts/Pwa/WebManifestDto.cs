using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Pwa;

/// <summary>
/// Manifest webovej aplikacie kiosku (tabula, stena tabul, virtualna tabula) - po nainstalovani na plochu
/// sa spusti rovno na kiosku na celu obrazovku. Kluce podla specifikacie Web App Manifest.
/// </summary>
public sealed record WebManifestDto
{
    /// <summary>Totoznost aplikacie - kazdy kiosk je samostatna aplikacia (bez kluca zariadenia).</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = "/";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = "";

    /// <summary>Adresa po spusteni - kiosk a pripadne kluc zariadenia.</summary>
    [JsonPropertyName("start_url")]
    public string StartUrl { get; init; } = "/";

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = "/";

    [JsonPropertyName("display")]
    public string Display { get; init; } = "fullscreen";

    [JsonPropertyName("display_override")]
    public IReadOnlyList<string> DisplayOverride { get; init; } = ["fullscreen", "standalone"];

    [JsonPropertyName("orientation")]
    public string Orientation { get; init; } = "any";

    [JsonPropertyName("background_color")]
    public string BackgroundColor { get; init; } = "#000000";

    [JsonPropertyName("theme_color")]
    public string ThemeColor { get; init; } = "#000000";

    [JsonPropertyName("icons")]
    public IReadOnlyList<WebManifestIconDto> Icons { get; init; } = [];
}
