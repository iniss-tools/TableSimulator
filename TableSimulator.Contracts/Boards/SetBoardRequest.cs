namespace TableSimulator.Contracts.Boards;

/// <summary>Nastavenie tabule (PUT /api/boards/{line}/{address}).</summary>
public sealed record SetBoardRequest
{
    /// <summary>Cislo linky; pri PUT plati cislo z adresy.</summary>
    public int Line { get; init; }

    /// <summary>Adresa na linke; pri PUT plati adresa z adresy.</summary>
    public int Address { get; init; }

    /// <summary>Kluc vyrobcu (<c>MANUFACTURER_KEY</c>).</summary>
    public string Manufacturer { get; init; } = "";

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Profil.</summary>
    public BoardProfileDto Profile { get; init; } = new();

    /// <summary>Vzhlad; null = predvoleny.</summary>
    public string? Design { get; init; }

    /// <summary>Predstierana porucha.</summary>
    public BoardFaultDto Fault { get; init; } = new(FaultKind.None);

    /// <summary>Druha adresa panelu; null = len jedna.</summary>
    public int? SecondAddress { get; init; }
}
