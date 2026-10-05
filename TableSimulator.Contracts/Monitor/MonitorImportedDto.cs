namespace TableSimulator.Contracts.Monitor;

/// <summary>Vysledok importu projektu ELEN Monitora.</summary>
/// <param name="Count">pocet zalozenych alebo zmenenych tabul</param>
public sealed record MonitorImportedDto(int Count);
