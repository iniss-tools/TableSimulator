namespace TableSimulator.Core.Boards;

/// <summary>
/// Obsah listovej tabule (ERS, FERS): kody listov po poziciach v riadkoch zhora od 0 - kazda pozicia je modul, ktory sa
/// natoci na list s danym kodom. Co je na liste, urcuju zoznamy listov profilu (<see cref="FlapProfile" />).
/// </summary>
public sealed class FlapDisplay : BoardDisplay
{
    /// <summary>Stav ERS "dokoncene v poriadku".</summary>
    public const int ErsDone = 1;

    private readonly Dictionary<int, string> _rows = [];

    /// <summary>Riadky s kodmi listov (pozicia = znak); kluc = riadok zhora od 0.</summary>
    public IReadOnlyDictionary<int, string> Rows => _rows;

    /// <summary>Do kedy sa listy otacaju; null = stoja.</summary>
    public DateTimeOffset? RotatingUntil { get; set; }

    /// <summary>Stav poslednej operacie pre dotaz ERS (01 hotovo, 02 chybne cislo riadku…).</summary>
    public int ErsStatus { get; set; } = ErsDone;

    /// <summary>Listy sa v case <paramref name="now" /> este otacaju.</summary>
    public bool IsRotating(DateTimeOffset now) => RotatingUntil is { } until && now < until;

    /// <summary>Zapise kody od pozicie (riadok sa doplni medzerami); vrati true, ak sa niektory list zmenil.</summary>
    public bool Write(int row, int position, string codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        var old = _rows.GetValueOrDefault(row, "");
        var line = old.PadRight(position + codes.Length);
        var updated = string.Concat(line.AsSpan(0, position), codes, line.AsSpan(position + codes.Length));
        _rows[row] = updated;
        return !string.Equals(old.PadRight(updated.Length), updated, StringComparison.Ordinal);
    }

    /// <summary>Vsetky moduly na prazdny list; vrati true, ak nejaky list nebol prazdny.</summary>
    public bool ClearAll()
    {
        var changed = _rows.Values.Any(r => r.Trim().Length > 0);
        _rows.Clear();
        return changed;
    }

    public override void Clear() => _rows.Clear();
}

/// <summary>
/// Texty listov v riadku listovej tabule podla profilu - pre porovnanie zaznamov aj zobrazenie. Pozicie, ktore ziadny
/// modul profilu nezabera, su samostatne moduly; kod bez zoznamu sa ukaze, ako prisiel, prazdny list je "".
/// </summary>
public static class FlapLabels
{
    /// <summary>Moduly riadku zlava doprava.</summary>
    /// <param name="profile">profil tabule</param>
    /// <param name="row">riadok tabule zhora od 0</param>
    /// <param name="positions">pocet pozicii v riadku (aspon)</param>
    public static IReadOnlyList<FlapModule> Modules(BoardProfile profile, int row, int positions)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var flaps = profile.Flaps;
        var line = flaps is null ? 0 : row % Math.Max(1, flaps.LinesPerRecord);
        var modules = flaps?.Modules.Where(m => m.Line == line).OrderBy(m => m.Position).ToList() ?? [];
        var width = Math.Max(positions, modules.Count == 0 ? 0 : modules.Max(m => m.Position + m.Span));
        var result = new List<FlapModule>();
        var next = 0;
        foreach (var module in modules)
        {
            for (; next < module.Position; next++)
                result.Add(new FlapModule(line, next, 1, null));
            if (module.Position < next)
                continue;
            result.Add(module);
            next = module.Position + module.Span;
        }

        for (; next < width; next++)
            result.Add(new FlapModule(line, next, 1, null));
        return result;
    }

    /// <summary>Text listu modulu pre kody riadku.</summary>
    /// <param name="profile">profil tabule</param>
    /// <param name="module">modul</param>
    /// <param name="codes">kody riadku</param>
    /// <param name="blankCode">kod prazdneho listu okrem medzery (ERS <c>0</c>); null = len medzera</param>
    public static string Label(BoardProfile profile, FlapModule module, string codes, char? blankCode = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(codes);
        var code = (module.Position < codes.Length ? codes[module.Position..Math.Min(codes.Length, module.Position + module.Span)] : "")
            .PadRight(module.Span);
        if (module.List is { } name && profile.Flaps?.Lists.GetValueOrDefault(name) is { } list)
        {
            if (list.TryGetValue(code, out var text) || list.TryGetValue(code.Trim(), out text))
                return text;
        }

        var trimmed = code.Trim();
        return trimmed.Length == 0 || (blankCode is { } blank && trimmed.All(c => c == blank)) ? "" : trimmed;
    }

    /// <summary>Kod prazdneho listu vyrobcu okrem medzery: ERS posiela namiesto medzier <c>0</c>.</summary>
    public static char? BlankCode(string manufacturer) => string.Equals(manufacturer, "ERS", StringComparison.OrdinalIgnoreCase) ? '0' : null;
}
