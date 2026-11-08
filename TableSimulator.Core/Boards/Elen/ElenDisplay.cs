namespace TableSimulator.Core.Boards.Elen;

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
