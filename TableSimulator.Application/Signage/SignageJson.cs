using System.Text.Json;

namespace TableSimulator.Application.Signage;

/// <summary>Obsah sprav, playlistov a skupin v databaze - JSON ako v API (kluce camelCase, enumy podla kontraktov).</summary>
public static class SignageJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException("Empty signage definition.");
}
