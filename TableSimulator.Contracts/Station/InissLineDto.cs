namespace TableSimulator.Contracts.Station;

/// <summary>Linka k tabuliam (sekcia Driver*) konfiguracie INISSu s tabulami, ktore jej INISS priradi.</summary>
/// <param name="Section">sekcia (Driver, Driver0…)</param>
/// <param name="Number">cislo linky; null = INISS ho z TablePort nezisti</param>
/// <param name="TableClass">trieda linky</param>
/// <param name="Port">TablePort - kam INISS posiela</param>
/// <param name="SimulatorPort">port linky v simulatore (z TablePort presmerovaneho INISSu, inak 47000 + cislo)</param>
/// <param name="Active">INISS linku zalozi</param>
/// <param name="Supported">simulator triedu linky pozna</param>
/// <param name="Tables">tabule linky</param>
/// <param name="Problems">zistenia</param>
public sealed record InissLineDto(
    string Section,
    int? Number,
    int TableClass,
    string Port,
    int SimulatorPort,
    bool Active,
    bool Supported,
    IReadOnlyList<InissTableDto> Tables,
    IReadOnlyList<InissProblemDto> Problems)
{
    /// <summary>Spinacie jednotky ELSVO na linke (okruhy Audio.txt so stlpcom 8 E a cislom linky).</summary>
    public IReadOnlyList<InissAmplifierDto> Amplifiers { get; init; } = [];

    /// <summary>Ako INISS na linku posiela.</summary>
    public StationLineTransport Transport { get; init; }

    /// <summary>INISS2: linku sa da presmerovat na simulator zmenou adresy.</summary>
    public bool CanRedirect { get; init; }

    /// <summary>INISS2: povodna adresa presmerovanej linky; null = linka nie je presmerovana.</summary>
    public string? OriginalPort { get; init; }
}
