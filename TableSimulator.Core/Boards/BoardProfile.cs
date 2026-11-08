using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Boards.Chars;
using TableSimulator.Core.Boards.Faces;
using TableSimulator.Core.Boards.Flaps;

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

    /// <summary>Ustredna AK Signal: svetla, rozhlas, snimac tmy a stavy; null = <see cref="AkSignalProfile.Default" />.</summary>
    public AkSignalProfile? AkSignal { get; init; }

    /// <summary>Profil pre novu tabulu daneho vyrobcu.</summary>
    public static BoardProfile DefaultFor(string manufacturerKey) => manufacturerKey.ToUpperInvariant() switch
    {
        "ELEN16" or "ELEN16KAM" => new BoardProfile { Width = 384, Rows = 4, RowHeight = 16 },
        "ELEKON" or "ELEKONKAM" => new BoardProfile { Width = 384, Rows = 4, RowHeight = 16 },
        // znakove tabule: 8 bodov na znak, 40 znakov
        "LCD1" or "ERP" or "LCD" or "ERP_ELEN" => new BoardProfile { Width = 320, Rows = 4, RowHeight = 16 },
        // listove tabule: pozicie po 8 bodoch, ERS predvolene 9 modulov v riadku (ERSLineLen)
        "FERS" or "ERS" => new BoardProfile { Width = 9 * 8, Rows = 4, RowHeight = 16 },
        // APEL: jednoriadkova nastupistna, viacriadkova odchodova - znaky po 8 bodoch, rozmer doladi samoucenie
        "APEL" => new BoardProfile { Width = 30 * 8, Rows = 1, RowHeight = 16 },
        "APEL_AN" => new BoardProfile { Width = 40 * 8, Rows = 6, RowHeight = 16 },
        _ => new BoardProfile()
    };
}
