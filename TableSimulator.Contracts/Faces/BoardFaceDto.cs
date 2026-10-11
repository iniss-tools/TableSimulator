namespace TableSimulator.Contracts.Faces;

/// <summary>
/// Predloha cela tabule - plochy, popisy, panely a hodiny okolo matice. Vstavane predlohy sa daju skopirovat
/// a upravit, vlastne su v databaze servera. Polohy a rozmery su v bodoch matice.
/// </summary>
public sealed record BoardFaceDto
{
    /// <summary>Kluc predlohy (male pismena bez diakritiky, cislice, pomlcka).</summary>
    public string Id { get; init; } = "";

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Vstavana predloha - neda sa zmenit ani odstranit (pri zmene sa ignoruje).</summary>
    public bool BuiltIn { get; init; }

    /// <summary>Miesto okolo matice pre prvky cela.</summary>
    public FaceMarginDto Margin { get; init; } = new();

    /// <summary>Medzera medzi riadkami tabule; null = podla vzhladu.</summary>
    public int? RowGap { get; init; }

    /// <summary>Pocet riadkov v skupine; 0 = bez skupin.</summary>
    public int RowGroup { get; init; }

    /// <summary>Medzera navyse medzi skupinami riadkov.</summary>
    public int GroupGap { get; init; }

    /// <summary>Sirka modulu v bodoch - na hraniciach modulov su svy (farbu ma vzhlad); 0 = bez svov.</summary>
    public int ModuleWidth { get; init; }

    /// <summary>Prvky cela v poradi kreslenia.</summary>
    public IReadOnlyList<FaceElementDto> Elements { get; init; } = [];
}
