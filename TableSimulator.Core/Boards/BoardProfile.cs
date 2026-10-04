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

    /// <summary>Ako dlho sa zobrazuje jedna cast striedania [ms].</summary>
    public int AlternationMs { get; init; } = 3000;

    /// <summary>Ako dlho blikajuci text svieti [ms].</summary>
    public int BlinkOnMs { get; init; } = 500;

    /// <summary>Ako dlho blikajuci text nesvieti [ms].</summary>
    public int BlinkOffMs { get; init; } = 500;

    /// <summary>Rychlost pohybliveho textu [body/s].</summary>
    public int ScrollSpeed { get; init; } = 40;

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
