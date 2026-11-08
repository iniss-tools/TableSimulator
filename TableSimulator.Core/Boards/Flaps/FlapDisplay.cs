namespace TableSimulator.Core.Boards.Flaps;

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
