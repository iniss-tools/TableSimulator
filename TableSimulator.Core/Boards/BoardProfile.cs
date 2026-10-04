using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards;

/// <summary>
/// Profil tabule - hardver a spravanie, ktore INISS neurcuje: rozmer matice, pisma a casovanie.
/// Ako tabula vyzera (tvar LED, farby, ram), urcuje vzhlad - ten je mimo jadra.
/// </summary>
public sealed record BoardProfile
{
    /// <summary>Sirka tabule v bodoch.</summary>
    public int Width { get; init; } = 192;

    /// <summary>Pocet riadkov (pri ELEN cislovanych odspodu od 1).</summary>
    public int Rows { get; init; } = 2;

    /// <summary>Vyska riadku v bodoch.</summary>
    public int RowHeight { get; init; } = 10;

    /// <summary>Subor pisiem ELEN (.bin) v priecinku pisiem; null = priblizne pismo prehliadaca.</summary>
    public string? FontFile { get; init; }

    /// <summary>
    /// Ktore pismo suboru sa pouzije pre rez 0-3 a rozsirene pismo 1-15 (kluc <c>face0</c>…<c>face3</c>, <c>ext1</c>…);
    /// chybajuci kluc = rez n → pismo n, rozsirene pismo n → pismo n.
    /// </summary>
    public IReadOnlyDictionary<string, int>? FontMap { get; init; }

    /// <summary>Ako dlho sa zobrazuje jedna cast striedania [ms] - ELEN Monitor prepina kazde 2 s.</summary>
    public int AlternationMs { get; init; } = 2000;

    /// <summary>Ako dlho blikajuci text svieti [ms] - FlashON konfiguracie panelu (predvolene 10 × 0,1 s).</summary>
    public int BlinkOnMs { get; init; } = 1000;

    /// <summary>Ako dlho blikajuci text nesvieti [ms] - FlashOFF konfiguracie panelu (predvolene 5 × 0,1 s).</summary>
    public int BlinkOffMs { get; init; } = 500;

    /// <summary>Rychlost pohybliveho textu [body/s] - ELEN Monitor posuva o bod kazdy tik (asi 16 ms).</summary>
    public int ScrollSpeed { get; init; } = 60;

    /// <summary>Jednofarebny panel: kazdy rozsvieteny bod ma farbu <c>mono</c> zo vzhladu (napr. zlte panely YC).</summary>
    public bool Monochrome { get; init; }

    /// <summary>Panel ma zamenenu cervenu a zelenu (konfiguracia panelu s farebnostou 2).</summary>
    public bool SwapRedGreen { get; init; }

    /// <summary>Hlavicka tabule - stitok nad maticou s textami a hodinami; null = bez hlavicky.</summary>
    public BoardHeader? Header { get; init; }

    /// <summary>Pismo pohybliveho textu.</summary>
    public int MovingFont { get; init; } = 0x52;

    /// <summary>Profil sa zvacsuje podla toho, co INISS posiela (tabula vytvorena samoucenim).</summary>
    public bool AutoSize { get; init; }

    /// <summary>Profil pre novu tabulu daneho vyrobcu.</summary>
    public static BoardProfile DefaultFor(string manufacturerKey) => manufacturerKey.ToUpperInvariant() switch
    {
        "ELEN16" or "ELEN16KAM" => new BoardProfile { Width = 384, Rows = 4, RowHeight = 16 },
        "ELEKON" => new BoardProfile { Width = 384, Rows = 4, RowHeight = 16 },
        _ => new BoardProfile()
    };
}

/// <summary>
/// Hlavicka tabule: pevny stitok nad maticou (napr. "ODCHODY / DEPARTURES" a popisy stlpcov) a hodiny panelu.
/// INISS ju neposiela - patri k hardveru tabule. Polohy a vysky su v bodoch matice.
/// </summary>
public sealed record BoardHeader
{
    /// <summary>Vyska hlavicky v bodoch.</summary>
    public int Height { get; init; }

    /// <summary>Texty hlavicky.</summary>
    public IReadOnlyList<HeaderText> Texts { get; init; } = [];

    /// <summary>Hodiny panelu; null = panel hodiny nema.</summary>
    public HeaderClock? Clock { get; init; }
}

/// <summary>
/// Text hlavicky.
/// </summary>
/// <param name="X">lavy okraj v bodoch</param>
/// <param name="Y">horny okraj v bodoch od vrchu hlavicky</param>
/// <param name="Height">vyska pisma v bodoch</param>
/// <param name="Text">text</param>
public sealed record HeaderText(int X, int Y, int Height, string Text);

/// <summary>
/// Hodiny v hlavicke - ukazuju cas hodin panelu, ktory nastavuje INISS (kym ho nenastavi, cas pocitaca).
/// </summary>
/// <param name="X">lavy okraj v bodoch</param>
/// <param name="Y">horny okraj v bodoch od vrchu hlavicky</param>
/// <param name="Height">vyska cislic v bodoch</param>
public sealed record HeaderClock(int X, int Y, int Height);

/// <summary>
/// Porucha tabule, ktoru simulator predstiera - na skusku reakcie INISSu.
/// </summary>
public enum FaultKind
{
    /// <summary>Bez poruchy.</summary>
    [JsonStringEnumMemberName("none")] None,

    /// <summary>Tabula neodpoveda.</summary>
    [JsonStringEnumMemberName("noReply")] NoReply,

    /// <summary>Tabula kazdu spravu odmietne (NAK).</summary>
    [JsonStringEnumMemberName("nak")] Nak,

    /// <summary>Tabula odpovie oneskorene.</summary>
    [JsonStringEnumMemberName("delay")] Delay,

    /// <summary>Tabula odpovie nezmyslom.</summary>
    [JsonStringEnumMemberName("garbage")] Garbage
}

/// <summary>
/// Porucha tabule.
/// </summary>
/// <param name="Kind">druh</param>
/// <param name="DelayMs">oneskorenie odpovede pri <see cref="FaultKind.Delay" /></param>
public sealed record BoardFault(FaultKind Kind, int DelayMs = 0)
{
    /// <summary>Bez poruchy.</summary>
    public static BoardFault None { get; } = new(FaultKind.None);
}
