namespace TableSimulator.Core.Boards.Flaps;

/// <summary>Farby listov listovej tabule (kluce v JSON a na webe).</summary>
public static class FlapColors
{
    public const string Black = "black";
    public const string Red = "red";
    public const string Yellow = "yellow";
    public const string White = "white";

    /// <summary>Vsetky farby.</summary>
    public static IReadOnlyList<string> All { get; } = [Black, Red, Yellow, White];

    /// <summary>
    /// Farby z <paramref name="previous" />, ktorych zoznam a kod su aj v novych zoznamoch - novy import zoznamov
    /// z dat stanice farby nastavene v simulatore zachova.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Keep(FlapProfile? previous, FlapProfile next)
    {
        ArgumentNullException.ThrowIfNull(next);
        var kept = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (list, colors) in previous?.Colors ?? new Dictionary<string, IReadOnlyDictionary<string, string>>())
        {
            if (!next.Lists.TryGetValue(list, out var codes))
                continue;
            var still = colors.Where(c => codes.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => c.Value, StringComparer.Ordinal);
            if (still.Count > 0)
                kept[list] = still;
        }

        foreach (var (list, colors) in next.Colors)
            kept[list] = colors;
        return kept;
    }
}
