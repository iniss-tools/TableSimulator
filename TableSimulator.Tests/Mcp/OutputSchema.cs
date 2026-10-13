using System.Text.Json;

namespace TableSimulator.Tests.Mcp;

/// <summary>
/// Kontrola vysledku nastroja voci jeho scheme vystupu - podmnozina JSON Schema, ktoru generuje SDK: typ (aj s null),
/// povinne a vnorene vlastnosti, polozky pola, slovnik, enum a odkaz <c>$ref</c> v ramci schemy.
/// </summary>
internal static class OutputSchema
{
    public static List<string> Validate(JsonElement value, JsonElement schema)
    {
        var errors = new List<string>();
        Check(value, schema, schema, "$", errors);
        return errors;
    }

    private static void Check(JsonElement value, JsonElement schema, JsonElement root, string path, List<string> errors)
    {
        if (schema.TryGetProperty("$ref", out var reference))
            schema = Resolve(root, reference.GetString()!);
        if (schema.TryGetProperty("type", out var type))
        {
            var types = type.ValueKind == JsonValueKind.Array ? type.EnumerateArray().Select(t => t.GetString()!).ToList() : [type.GetString()!];
            if (!types.Any(t => Matches(value, t)))
            {
                errors.Add($"{path}: {value.ValueKind} nie je {string.Join("|", types)}");
                return;
            }
        }

        if (value.ValueKind == JsonValueKind.Null)
            return;
        if (schema.TryGetProperty("enum", out var values) && !values.EnumerateArray().Any(v => v.ValueKind == value.ValueKind && v.ToString() == value.ToString()))
            errors.Add($"{path}: {value} nie je medzi hodnotami enum");
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out var required))
            {
                foreach (var name in required.EnumerateArray().Select(r => r.GetString()!).Where(n => !value.TryGetProperty(n, out _)))
                    errors.Add($"{path}.{name}: chyba povinna vlastnost");
            }

            var properties = schema.TryGetProperty("properties", out var p) ? p : default;
            foreach (var property in value.EnumerateObject())
            {
                if (properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty(property.Name, out var propertySchema))
                    Check(property.Value, propertySchema, root, $"{path}.{property.Name}", errors);
                else if (schema.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.Object)
                    Check(property.Value, additional, root, $"{path}.{property.Name}", errors);
            }
        }

        if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
                Check(item, items, root, $"{path}[{index++}]", errors);
        }
    }

    /// <summary>Odkaz <c>#/a/b</c> v ramci schemy.</summary>
    private static JsonElement Resolve(JsonElement root, string reference)
    {
        var node = root;
        foreach (var part in reference.TrimStart('#').Split('/', StringSplitOptions.RemoveEmptyEntries))
            node = node.ValueKind == JsonValueKind.Array ? node[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)] : node.GetProperty(part.Replace("~1", "/").Replace("~0", "~"));
        return node;
    }

    private static bool Matches(JsonElement value, string type) => type switch
    {
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "string" => value.ValueKind == JsonValueKind.String,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        "number" => value.ValueKind == JsonValueKind.Number,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "null" => value.ValueKind == JsonValueKind.Null,
        _ => true
    };
}
