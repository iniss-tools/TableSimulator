using System.Text.Json;
using System.Text.Json.Serialization;

namespace TableSimulator.Infrastructure;

/// <summary>Nastavenia JSON servera.</summary>
public static class JsonDefaults
{
    /// <summary>Subory nastavenia a konfiguracie liniek (typy jadra s klucmi enumov <c>JsonStringEnumMemberName</c>).</summary>
    public static JsonSerializerOptions Files { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Spravy WebSocketu - kontrakty (enumy maju vlastny prevodnik).</summary>
    public static JsonSerializerOptions Wire { get; } = new(JsonSerializerDefaults.Web);
}
