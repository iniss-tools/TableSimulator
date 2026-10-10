namespace TableSimulator.Core.Boards;

/// <summary>
/// Obsah tabule - podla rodiny protokolu.
/// </summary>
public abstract class BoardDisplay
{
    /// <summary>Jas 0-100; 0 = zhasnuta.</summary>
    public int Brightness { get; set; } = 100;

    /// <summary>Cas hodin tabule, ako ho naposledy nastavil INISS; null = nenastaveny.</summary>
    public DateTime? ClockValue { get; set; }

    /// <summary>Kedy INISS hodiny nastavil.</summary>
    public DateTimeOffset? ClockSetAt { get; set; }

    /// <summary>Zmaze obsah (po zapnuti alebo prikazom zmazania).</summary>
    public abstract void Clear();
}

/// <summary>
/// Obsah maticovej tabule ELEN: riadky cislovane odspodu, pohyblivy text a jas.
/// </summary>
public sealed class ElenDisplay : BoardDisplay
{
    private readonly Dictionary<int, RowContent> _rows = [];

    /// <summary>Riadky s obsahom; kluc = cislo riadku odspodu (1 = spodny).</summary>
    public IReadOnlyDictionary<int, RowContent> Rows => _rows;

    /// <summary>Riadok s pohyblivym textom odspodu; 0 = ziadny.</summary>
    public int MovingRow { get; set; }

    /// <summary>Bajty pohybliveho textu (bez hlavicky "T0" a oddelovacov).</summary>
    public byte[]? MovingCodes { get; set; }

    /// <summary>Pohyblivy text dekodovany.</summary>
    public string? MovingText { get; set; }

    /// <summary>Rozpracovane nahravanie pohybliveho textu.</summary>
    internal List<byte> MovingUpload { get; } = [];

    /// <summary>Jas zelenej 0-100 (prikaz 4 s dvoma bajtmi); <see cref="BoardDisplay.Brightness" /> je jas cervenej.</summary>
    public int BrightnessGreen { get; set; } = 100;

    /// <summary>Ako dlho blikajuci text svieti [× 0,1 s], ak ho INISS nastavil prikazom 9; null = podla profilu.</summary>
    public int? FlashOn { get; set; }

    /// <summary>Ako dlho blikajuci text nesvieti [× 0,1 s], ak ho INISS nastavil prikazom 9; null = podla profilu.</summary>
    public int? FlashOff { get; set; }

    /// <summary>Timeout panelu [s] z prikazu 8; null = nenastaveny.</summary>
    public int? Timeout { get; set; }

    /// <summary>Nastavi obsah riadku; prazdny obsah riadok zmaze.</summary>
    public void SetRow(int row, RowContent content)
    {
        if (content.IsEmpty)
            _rows.Remove(row);
        else
            _rows[row] = content;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        _rows.Clear();
        MovingCodes = null;
        MovingText = null;
        MovingUpload.Clear();
    }
}

/// <summary>
/// Pocitadla komunikacie tabule.
/// </summary>
public sealed class BoardStats
{
    private DateTimeOffset? _lastFrameAt;
    private long? _maxGapMs;

    public long Frames { get; set; }
    public long Acks { get; set; }
    public long Naks { get; set; }
    public long Unanswered { get; set; }

    /// <summary>Posledna sprava pre tabulu; nova hodnota zaznamena pauzu od predchadzajucej (statistika).</summary>
    public DateTimeOffset? LastFrameAt
    {
        get => _lastFrameAt;
        set
        {
            if (_lastFrameAt is { } last && value is { } now && now > last)
            {
                var gap = (long)(now - last).TotalMilliseconds;
                if (_maxGapMs is not { } max || gap > max)
                    _maxGapMs = gap;
            }

            _lastFrameAt = value;
        }
    }

    /// <summary>Najdlhsia pauza medzi spravami od posledneho odberu [ms] a jej vynulovanie; null = ziadna.</summary>
    internal long? TakeMaxGap()
    {
        var gap = _maxGapMs;
        _maxGapMs = null;
        return gap;
    }
}

/// <summary>
/// INISS posiela tabuli spravy pre ineho vyrobcu, nez ma tabula v simulatore nastaveneho (napr. rozsirene pismo
/// tabuli ELEN) - tabula potom ukazuje obsah zle.
/// </summary>
/// <param name="Evidence">co na to ukazuje (kluc pre texty rozhrania, napr. <c>elen.extendedFont</c>)</param>
/// <param name="Suggested">vyrobcovia, ktorym take spravy patria</param>
public sealed record ManufacturerMismatch(string Evidence, IReadOnlyList<string> Suggested)
{
    /// <summary>Rovnake zistenie (zoznamy porovna po prvkoch).</summary>
    public bool Equals(ManufacturerMismatch? other) =>
        other is not null && Evidence == other.Evidence && Suggested.SequenceEqual(other.Suggested);

    /// <inheritdoc />
    public override int GetHashCode() => Evidence.GetHashCode(StringComparison.Ordinal);
}

/// <summary>
/// Simulovana tabula na linke.
/// </summary>
public sealed class SimBoard
{
    public SimBoard(int line, int address, string name, string manufacturerKey, BoardProfile profile, BoardDisplay display)
    {
        Line = line;
        Address = address;
        Name = name;
        ManufacturerKey = manufacturerKey;
        Profile = profile;
        Display = display;
    }

    /// <summary>Identifikator tabule: <c>linka-adresa</c>.</summary>
    public string Id => MakeId(Line, Address);

    /// <summary>Cislo linky.</summary>
    public int Line { get; }

    /// <summary>Adresa tabule na linke.</summary>
    public int Address { get; }

    /// <summary>Druha adresa panelu (dvojita adresa) - ovlada hornu polovicu riadkov; null = len jedna adresa.</summary>
    public int? SecondAddress { get; set; }

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; set; }

    /// <summary>Kluc vyrobcu (<c>MANUFACTURER_KEY</c>), urcuje variant protokolu.</summary>
    public string ManufacturerKey { get; set; }

    /// <summary>Profil - hardver tabule.</summary>
    public BoardProfile Profile { get; set; }

    /// <summary>Identifikator vzhladu; null = predvoleny pre technologiu.</summary>
    public string? Design { get; set; }

    /// <summary>Predstierana porucha.</summary>
    public BoardFault Fault { get; set; } = BoardFault.None;

    /// <summary>Spravy od INISSu patria inemu vyrobcovi; null = nic take sa nezistilo.</summary>
    public ManufacturerMismatch? Mismatch { get; internal set; }

    /// <summary>Predstierany vypadok napajania: tabula nic neprijima ani neodpoveda, po navrate je prazdna.</summary>
    public bool PoweredOff { get; internal set; }

    /// <summary>Tabulu vytvorilo samoucenie (prva sprava na neznamu adresu).</summary>
    public bool AutoCreated { get; init; }

    /// <summary>Obsah tabule.</summary>
    public BoardDisplay Display { get; }

    /// <summary>Pocitadla komunikacie (pri zmene nastavenia tabule ostanu).</summary>
    public BoardStats Stats { get; init; } = new();

    /// <summary>Zvysi sa pri kazdej zmene obsahu alebo nastavenia.</summary>
    public long Version { get; internal set; }

    /// <summary>Identifikator tabule z linky a adresy.</summary>
    public static string MakeId(int line, int address) => $"{line}-{address}";
}
