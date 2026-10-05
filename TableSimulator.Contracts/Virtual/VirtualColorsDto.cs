namespace TableSimulator.Contracts.Virtual;

/// <summary>Farby virtualnej tabule (<c>#rrggbb</c>).</summary>
public sealed record VirtualColorsDto
{
    /// <summary>Pozadie tabule.</summary>
    public string Background { get; init; } = "#0b1a3a";

    /// <summary>Text zaznamov.</summary>
    public string Text { get; init; } = "#ffffff";

    /// <summary>Pozadie hlavicky.</summary>
    public string HeadBackground { get; init; } = "#06102a";

    /// <summary>Nadpis v hlavicke.</summary>
    public string HeadText { get; init; } = "#ffffff";

    /// <summary>Zvyraznenie - hodiny, ciara pod hlavickou, stlpec s farbou <c>accent</c>.</summary>
    public string Accent { get; init; } = "#ffcc00";

    /// <summary>Tlmeny text - stanica v hlavicke, popisy stlpcov, stlpec s farbou <c>muted</c>.</summary>
    public string Muted { get; init; } = "#9fb3d9";

    /// <summary>Meskanie - stlpec s farbou <c>delay</c>.</summary>
    public string Delay { get; init; } = "#ff8080";

    /// <summary>Ciara medzi zaznamami.</summary>
    public string RowLine { get; init; } = "#1a2d57";

    /// <summary>Pozadie kazdeho druheho zaznamu; prazdne = bez striedania.</summary>
    public string RowAlternate { get; init; } = "#0e2147";
}
