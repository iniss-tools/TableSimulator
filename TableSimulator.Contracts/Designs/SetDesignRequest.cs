namespace TableSimulator.Contracts.Designs;

/// <summary>
/// Vzhlad tabule (obsah <c>design.json</c>, PUT /api/designs/{id}) - vsetko, co urcuje, ako tabula vyzera.
/// </summary>
public sealed record SetDesignRequest
{
    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Kreslenie - <c>led-matrix</c> (body: LED aj LCD) alebo <c>flaps</c> (listove tabule).</summary>
    public string Renderer { get; init; } = "led-matrix";

    /// <summary>Vzdialenost stredov bodov v CSS px pri mierke 1.</summary>
    public double Pitch { get; init; }

    /// <summary>Priemer bodu ako podiel <see cref="Pitch" /> (0-1).</summary>
    public double Dot { get; init; }

    public DesignShape Shape { get; init; }

    /// <summary>Farba zhasnuteho bodu; null = nekreslit.</summary>
    public string? Off { get; init; }

    /// <summary>Farby podla farby pisma ELEN: <c>0</c> bez farby, <c>1</c> cervena, <c>2</c> zelena, <c>3</c> zlta, <c>mono</c> jednofarebny panel.</summary>
    public IReadOnlyDictionary<string, string> Colors { get; init; } = new Dictionary<string, string>();

    /// <summary>Ziara okolo rozsvieteneho bodu 0-1.</summary>
    public double Glow { get; init; }

    /// <summary>Pozadie pod bodmi (modul).</summary>
    public string Background { get; init; } = "";

    /// <summary>Plech cela okolo modulov; null = ako pozadie.</summary>
    public string? Plate { get; init; }

    /// <summary>Okraj modulu nad a pod riadkom bodov v bodoch (vidno ho, ked ma plech inu farbu nez pozadie).</summary>
    public double ModuleMargin { get; init; } = 1;

    /// <summary>Medzera medzi riadkami tabule v bodoch.</summary>
    public double RowGap { get; init; }

    /// <summary>Okraj medzi ramom a bodmi v bodoch.</summary>
    public double Padding { get; init; }

    public DesignFrameDto Frame { get; init; } = new(0, "#000000", 0);

    /// <summary>Vypln medzi riadkami; null = pozadie.</summary>
    public string? GapColor { get; init; }

    /// <summary>Svy medzi modulmi (rozostup urcuje celo tabule).</summary>
    public DesignSeamDto Seam { get; init; } = new(null, 0.5);

    /// <summary>Pismo a farby cela tabule.</summary>
    public DesignHeaderDto Header { get; init; } = new("#000000", "#ffffff", "sans-serif", true, "#ffffff", "#000000", "sans-serif");

    /// <summary>Material listovej tabule; null pri vzhlade bodov.</summary>
    public DesignFlapsDto? Flaps { get; init; }
}
