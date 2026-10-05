namespace TableSimulator.Contracts.Virtual;

/// <summary>Stlpec virtualnej tabule.</summary>
public sealed record VirtualColumnDto
{
    /// <summary>Prvky XML INISSu, ktore stlpec ukazuje (<c>time</c>, <c>type</c>, <c>number</c>, <c>destination</c>…).</summary>
    public IReadOnlyList<string> Cells { get; init; } = [];

    /// <summary>Popis stlpca; prazdny = predvoleny podla prvku v jazyku obsluhy.</summary>
    public string Label { get; init; } = "";

    /// <summary>Sirka: prazdna = podla obsahu, <c>fill</c> = zvysok riadku, inak <c>12%</c> alebo <c>6em</c>.</summary>
    public string Width { get; init; } = "";

    /// <summary>Zarovnanie textu.</summary>
    public VirtualAlign Align { get; init; } = VirtualAlign.Left;

    /// <summary>Farba: prazdna = text, <c>accent</c>, <c>muted</c>, <c>delay</c> z farieb rozlozenia, alebo <c>#rrggbb</c>.</summary>
    public string Color { get; init; } = "";

    /// <summary>Tucne pismo.</summary>
    public bool Bold { get; init; }

    /// <summary>Velkost pisma v percentach riadku.</summary>
    public int Size { get; init; } = 100;

    /// <summary>Text medzi prvkami stlpca (napr. druh a cislo vlaku).</summary>
    public string Separator { get; init; } = " ";
}
