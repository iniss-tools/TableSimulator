namespace TableSimulator.Core.Boards.Chars;

/// <summary>
/// Znak znakovej tabule: kod (podla kodovania tabule) a cislo pisma; kod -1 = bunka vo vrstve nie je nastavena.
/// </summary>
/// <param name="Code">kod znaku 0-255; -1 = nenastavena</param>
/// <param name="Font">cislo pisma znaku</param>
public readonly record struct CharCell(short Code, byte Font)
{
    /// <summary>Nenastavena bunka (vrstvy striedania nesu len zmenene znaky).</summary>
    public static CharCell Unset { get; } = new(-1, 0);

    /// <summary>Bunka je nastavena.</summary>
    public bool IsSet => Code >= 0;
}
