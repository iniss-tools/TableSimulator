namespace TableSimulator.Contracts.Boards;

/// <summary>Profil tabule - hardver a spravanie, ktore INISS neurcuje (rozmer, pisma, casovanie, hlavicka).</summary>
public sealed record BoardProfileDto
{
    /// <summary>Sirka v bodoch.</summary>
    public int Width { get; init; } = 192;

    /// <summary>Pocet riadkov.</summary>
    public int Rows { get; init; } = 2;

    /// <summary>Vyska riadku v bodoch.</summary>
    public int RowHeight { get; init; } = 10;

    /// <summary>Subor pisiem ELEN (.bin); null = priblizne pismo prehliadaca.</summary>
    public string? FontFile { get; init; }

    /// <summary>Ktore pismo suboru sa pouzije pre rez (<c>face0</c>…) a rozsirene pismo (<c>ext1</c>…); null = podla cisla.</summary>
    public IReadOnlyDictionary<string, int>? FontMap { get; init; }

    /// <summary>Jedna cast striedania [ms].</summary>
    public int AlternationMs { get; init; } = 2000;

    /// <summary>Blikajuci text svieti [ms].</summary>
    public int BlinkOnMs { get; init; } = 1000;

    /// <summary>Blikajuci text nesvieti [ms].</summary>
    public int BlinkOffMs { get; init; } = 500;

    /// <summary>Rychlost pohybliveho textu [body/s].</summary>
    public int ScrollSpeed { get; init; } = 60;

    /// <summary>Jednofarebny panel.</summary>
    public bool Monochrome { get; init; }

    /// <summary>Panel so zamenenou cervenou a zelenou.</summary>
    public bool SwapRedGreen { get; init; }

    /// <summary>Hlavicka; null = bez hlavicky.</summary>
    public BoardHeaderDto? Header { get; init; }

    /// <summary>Pismo pohybliveho textu.</summary>
    public int MovingFont { get; init; } = 0x52;

    /// <summary>Profil sa zvacsuje podla obsahu (samoucenie).</summary>
    public bool AutoSize { get; init; }

    /// <summary>Listova tabula: ako dlho sa listy po zapise otacaju [ms].</summary>
    public int RotationMs { get; init; } = 3000;

    /// <summary>Listova tabula: moduly a zoznamy listov; null = kazda pozicia ukaze kod.</summary>
    public FlapProfileDto? Flaps { get; init; }
}
