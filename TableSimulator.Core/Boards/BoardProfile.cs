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

    /// <summary>
    /// Znakova tabula (LCD1, ERP): pismo podla cisla pisma znaku bez bitov inverzie a blikania (kluc <c>16</c>…);
    /// chybajuci kluc = predvolene pismo. null = vsetky znaky predvolenym pismom.
    /// </summary>
    public IReadOnlyDictionary<string, CharFont>? CharFonts { get; init; }

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

    /// <summary>
    /// Hlavicka tabule - stitok nad maticou s textami a hodinami; null = bez hlavicky. Starsi tvar cela: server ju pri
    /// ulozeni tabule prevedie na predlohu cela (<see cref="Face" />), ostava pre import z ELEN Monitora a stare subory.
    /// </summary>
    public BoardHeader? Header { get; init; }

    /// <summary>Predloha cela (<see cref="BoardFace.Id" />); null = bez cela.</summary>
    public string? Face { get; init; }

    /// <summary>Hodnoty premennych cela (<c>track</c> → <c>1</c>); null = ziadne.</summary>
    public IReadOnlyDictionary<string, string>? FaceValues { get; init; }

    /// <summary>Pismo pohybliveho textu.</summary>
    public int MovingFont { get; init; } = 0x52;

    /// <summary>Profil sa zvacsuje podla toho, co INISS posiela (tabula vytvorena samoucenim).</summary>
    public bool AutoSize { get; init; }

    /// <summary>
    /// Listova tabula: ako dlho sa listy po zapise otacaju [ms] - dovtedy tabula FERS neodpoveda a ERS hlasi, ze este
    /// pracuje.
    /// </summary>
    public int RotationMs { get; init; } = 3000;

    /// <summary>Listova tabula: moduly a zoznamy listov; null = kazda pozicia je samostatny modul a ukaze kod.</summary>
    public FlapProfile? Flaps { get; init; }

    /// <summary>Profil pre novu tabulu daneho vyrobcu.</summary>
    public static BoardProfile DefaultFor(string manufacturerKey) => manufacturerKey.ToUpperInvariant() switch
    {
        "ELEN16" or "ELEN16KAM" => new BoardProfile { Width = 384, Rows = 4, RowHeight = 16 },
        "ELEKON" => new BoardProfile { Width = 384, Rows = 4, RowHeight = 16 },
        // znakove tabule Elektrocas: 8 bodov na znak, 40 znakov
        "LCD1" or "ERP" or "LCD" => new BoardProfile { Width = 320, Rows = 4, RowHeight = 16 },
        // listove tabule: pozicie po 8 bodoch, ERS predvolene 9 modulov v riadku (ERSLineLen)
        "FERS" or "ERS" => new BoardProfile { Width = 9 * 8, Rows = 4, RowHeight = 16 },
        // APEL: jednoriadkova nastupistna, viacriadkova odchodova - znaky po 8 bodoch, rozmer doladi samoucenie
        "APEL" => new BoardProfile { Width = 30 * 8, Rows = 1, RowHeight = 16 },
        "APEL_AN" => new BoardProfile { Width = 40 * 8, Rows = 6, RowHeight = 16 },
        _ => new BoardProfile()
    };
}

/// <summary>
/// Moduly listovej tabule a zoznamy listov - INISS posiela kody listov, co na liste je, vie len tabula. Zostavuje sa
/// z katalogu tabule a prekodovacich tabuliek stanice (<c>ToolsCore.Iniss.Tables.FlapLayouts</c>).
/// </summary>
public sealed record FlapProfile
{
    /// <summary>Riadkov tabule na jeden zaznam - riadok modulu = riadok tabule modulo tento pocet.</summary>
    public int LinesPerRecord { get; init; } = 1;

    /// <summary>Moduly; pozicie, ktore ziadny modul nezabera, su samostatne moduly bez zoznamu.</summary>
    public IReadOnlyList<FlapModule> Modules { get; init; } = [];

    /// <summary>Zoznamy listov podla mena: kod listu → text (text zacinajuci <c>*</c> je na cervenom liste).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Lists { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();

    /// <summary>
    /// Farby listov nastavene v simulatore (zoznam → kod → <see cref="FlapColors" />) - fyzicka vlastnost listu, ktoru
    /// data stanice nemaju; list bez farby je cierny, text zacinajuci <c>*</c> cerveny.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Colors { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();
}

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

/// <summary>
/// Modul listovej tabule.
/// </summary>
/// <param name="Line">riadok v ramci zaznamu (od 0)</param>
/// <param name="Position">prva pozicia v riadku (znak spravy INISSu)</param>
/// <param name="Span">pocet pozicii - ich kody spolu urcuju list</param>
/// <param name="List">meno zoznamu listov; null = ukaze sa kod</param>
public sealed record FlapModule(int Line, int Position, int Span, string? List);

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
    [JsonStringEnumMemberName("garbage")] Garbage,

    /// <summary>Tabula hlasi stav s poruchou (tabule so stavovou odpovedou, napr. LCD1).</summary>
    [JsonStringEnumMemberName("status")] Status
}

/// <summary>
/// Porucha tabule.
/// </summary>
/// <param name="Kind">druh</param>
/// <param name="DelayMs">oneskorenie odpovede pri <see cref="FaultKind.Delay" /></param>
/// <param name="Status">stavove bity pri <see cref="FaultKind.Status" /> (napr. 0x08 porucha izolacie, 0x40 napajanie)</param>
public sealed record BoardFault(FaultKind Kind, int DelayMs = 0, int Status = 0)
{
    /// <summary>Bez poruchy.</summary>
    public static BoardFault None { get; } = new(FaultKind.None);
}
