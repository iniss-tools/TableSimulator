namespace TableSimulator.Contracts.Station;

/// <summary>Zistenie o linke alebo tabuli konfiguracie INISSu.</summary>
/// <param name="Severity">zavaznost</param>
/// <param name="Code">druh</param>
/// <param name="Args">parametre textu v poradi podla druhu</param>
public sealed record InissProblemDto(InissProblemSeverity Severity, InissProblemCode Code, IReadOnlyList<string> Args);
