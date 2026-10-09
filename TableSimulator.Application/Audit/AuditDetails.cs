using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TableSimulator.Application.Audit;

/// <summary>
/// Podrobnosti zaznamu auditu: prikaz ako JSON. Hodnota s heslom (vlastnost obsahujuca <c>password</c>) sa nahradi
/// <c>***</c>, dlhe texty sa skratia; velke obsahy (subory) prikazy z JSON vynechavaju samy.
/// </summary>
public static class AuditDetails
{
    /// <summary>Najdlhsie podrobnosti.</summary>
    public const int MaxLength = 2000;

    /// <summary>Najdlhsi text jednej hodnoty.</summary>
    public const int MaxText = 200;

    private const string Hidden = "***";

    // diakritika citatelne - podrobnosti zobrazuje webove rozhranie ako text, nie ako HTML; kluce slovnikov (napr. kody
    // listov C a c) rozlisuju velke a male pismena - uzol JSON by ich inak povazoval za ten isty kluc
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = false
    };

    /// <summary>Podrobnosti prikazu; prazdne, ak prikaz nema ziadne udaje.</summary>
    public static string Of(object command)
    {
        ArgumentNullException.ThrowIfNull(command);
        JsonNode? node;
        try
        {
            node = JsonSerializer.SerializeToNode(command, command.GetType(), Json);
        }
        catch (Exception e) when (e is NotSupportedException or JsonException or InvalidOperationException)
        {
            return "";
        }

        if (node is not JsonObject root || root.Count == 0)
            return "";
        Clean(root);
        var text = root.ToJsonString(Json);
        return text.Length <= MaxLength ? text : text[..MaxLength] + "…";
    }

    private static void Clean(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, value) in obj.ToArray())
                {
                    if (key.Contains("password", StringComparison.OrdinalIgnoreCase))
                        obj[key] = Hidden;
                    else if (Shortened(value) is { } text)
                        obj[key] = text;
                    else
                        Clean(value);
                }

                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (Shortened(array[i]) is { } text)
                        array[i] = text;
                    else
                        Clean(array[i]);
                }

                break;
        }
    }

    private static string? Shortened(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > MaxText ? text[..MaxText] + "…" : null;
}
