using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TableSimulator.Core.Simulation;

/// <summary>
/// Linky a tabule ako JSON - subor konfiguracie (export, import a povodny <c>config.json</c>) a profil tabule
/// v databaze. Enumy jadra maju kluce z <c>JsonStringEnumMemberName</c>, nie z nazvov C#.
/// </summary>
public static class SimulatorConfigJson
{
    /// <summary>Nastavenie JSON pre typy jadra (diakritika citatelne - subor a databaza, nie HTML).</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private static JsonSerializerOptions Indented { get; } = new(Options) { WriteIndented = true };

    /// <summary>Subor konfiguracie (citatelny, odsadeny).</summary>
    public static string Serialize(SimulatorConfig config) => JsonSerializer.Serialize(config, Indented);

    /// <summary>Konfiguracia zo suboru.</summary>
    /// <exception cref="JsonException">obsah nie je konfiguracia liniek a tabul</exception>
    public static SimulatorConfig Deserialize(string json) =>
        JsonSerializer.Deserialize<SimulatorConfig>(json, Options) ?? throw new JsonException("The configuration is empty.");

    /// <summary>Kluc hodnoty enumu jadra (ako v JSON).</summary>
    public static string Key<T>(T value) where T : struct, Enum => JsonSerializer.Serialize(value, Options).Trim('"');

    /// <summary>Hodnota enumu jadra podla kluca; neznamy kluc = predvolena hodnota.</summary>
    public static T Parse<T>(string key) where T : struct, Enum
    {
        try
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(key, Options), Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
