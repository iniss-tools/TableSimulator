namespace TableSimulator.Contracts.Station;

/// <summary>Linka k tabuliam (sekcia Driver*) konfiguracie INISSu s tabulami, ktore jej INISS priradi.</summary>
/// <param name="Section">sekcia (Driver, Driver0…)</param>
/// <param name="Number">cislo linky; null = INISS ho z TablePort nezisti</param>
/// <param name="TableClass">trieda linky</param>
/// <param name="Port">TablePort - kam INISS posiela</param>
/// <param name="SimulatorPort">port TCP linky v simulatore (z TablePort presmerovaneho INISSu, inak 47000 + cislo)</param>
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
    IReadOnlyList<InissProblemDto> Problems);
