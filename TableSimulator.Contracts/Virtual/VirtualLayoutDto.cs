namespace TableSimulator.Contracts.Virtual;

/// <summary>
/// Rozlozenie virtualnej tabule - ako vyzera (hlavicka, stlpce, farby, pismo, striedanie). Su to data: vstavane
/// rozlozenia sa daju skopirovat a upravit, vlastne su v databaze servera.
/// </summary>
public sealed record VirtualLayoutDto
{
    /// <summary>Kluc rozlozenia (male pismena bez diakritiky, cislice, pomlcka).</summary>
    public string Id { get; init; } = "";

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Vstavane rozlozenie - neda sa zmenit ani odstranit (pri zmene sa ignoruje).</summary>
    public bool BuiltIn { get; init; }

    /// <summary>Nadpis v hlavicke; <c>{name}</c> = nazov tabule, <c>{station}</c> = stanica z XML.</summary>
    public string Title { get; init; } = "{name}";

    /// <summary>V hlavicke aj nazov stanice z XML.</summary>
    public bool ShowStation { get; init; } = true;

    /// <summary>Hodiny v hlavicke.</summary>
    public VirtualClock Clock { get; init; } = VirtualClock.Seconds;

    /// <summary>V hlavicke aj datum.</summary>
    public bool ShowDate { get; init; }

    /// <summary>Riadok s popismi stlpcov.</summary>
    public bool ColumnHeaders { get; init; } = true;

    /// <summary>Pocet riadkov, na ktory sa prisposobi velkost pisma, ak tabula nema pevny pocet zaznamov.</summary>
    public int Rows { get; init; } = 8;

    /// <summary>Pismo (CSS font-family); prazdne = predvolene.</summary>
    public string Font { get; init; } = "";

    /// <summary>Farby.</summary>
    public VirtualColorsDto Colors { get; init; } = new();

    /// <summary>Stlpce; prazdne = podla XML (poradie stlpcov katalogovej tabule INISSu).</summary>
    public IReadOnlyList<VirtualColumnDto> Columns { get; init; } = [];

    /// <summary>Interval striedania casti textu oddelenych <c>#</c> v ms (INISSView Jscript.iNextString).</summary>
    public int AlternationMs { get; init; } = 1500;

    /// <summary>Dlzka jednej fazy blikania v ms (Jscript.iBlinkPause).</summary>
    public int BlinkMs { get; init; } = 600;

    /// <summary>Pocet faz blikania - v poslednej text nesvieti (Jscript.iMaxBlinkPhase).</summary>
    public int BlinkPhases { get; init; } = 3;
}
