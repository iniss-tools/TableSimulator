namespace TableSimulator.Contracts.Station;

/// <summary>Linka stanice INISSu.</summary>
/// <param name="Number">cislo linky (podla neho INISS priraduje tabule)</param>
/// <param name="TableClass">trieda linky</param>
/// <param name="Port">port TCP, na ktory INISS posiela (TablePort <c>N=TCP://host:port</c>)</param>
/// <param name="Name">nazov pre obsluhu (napr. sekcia Driver0)</param>
public sealed record StationLineDto(int Number, int TableClass, int Port, string Name = "");
