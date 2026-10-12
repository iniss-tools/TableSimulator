namespace TableSimulator.Core.Boards;

/// <summary>
/// Pismo znakovej tabule (LCD1, ERP) pre jedno cislo pisma - INISS cislo pisma kazdeho znaku len posiela, co znamena,
/// urcuje tabula.
/// </summary>
/// <param name="Font">bodove pismo (<c>5x7</c>, <c>6x10</c>…); prazdne = najvacsie, ktore sa zmesti do bunky znaku</param>
/// <param name="Bold">ztucnene o bod doprava</param>
public sealed record CharFont(string Font = "", bool Bold = false)
{
    /// <summary>Bodove pisma, ktore sa zmestia do bunky znaku (8 bodov vratane medzery).</summary>
    public static IReadOnlyList<string> Fonts { get; } = ["5x7", "5x8", "6x10", "6x12", "7x13", "7x14"];

    /// <summary>Najvacsie cislo pisma bez bitov inverzie (0x40) a blikania (0x80).</summary>
    public const int MaxNumber = 63;
}
