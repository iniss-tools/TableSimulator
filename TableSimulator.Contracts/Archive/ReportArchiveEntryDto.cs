namespace TableSimulator.Contracts.Archive;

/// <summary>Zaznam archivu hlaseni - hlasenie, zmena stavu vlaku, prihlasenie a ine udalosti.</summary>
/// <param name="Time">cas zapisu</param>
/// <param name="Kind">druh udalosti (text INISSu)</param>
/// <param name="Name">nazov hlasenia alebo udalosti</param>
/// <param name="Auto">spustila ho automatika</param>
/// <param name="Station">stanica</param>
/// <param name="Train">vlak</param>
/// <param name="ArrivalTrack">kolaj prichodu</param>
/// <param name="DepartureTrack">kolaj odchodu (ak sa lisi)</param>
/// <param name="ArrivalDelay">posun prichodu - predpokladane meskanie [min]</param>
/// <param name="DepartureDelay">posun odchodu [min] (ak sa lisi)</param>
/// <param name="Text">text hlasenia</param>
/// <param name="Result">vysledok (<c>OK</c>, <c>break</c>, <c>deleted</c>…)</param>
/// <param name="Details">ostatne atributy</param>
public sealed record ReportArchiveEntryDto(
    TimeOnly Time,
    string Kind,
    string Name,
    bool Auto,
    string Station,
    ArchiveTrainDto? Train,
    string ArrivalTrack,
    string DepartureTrack,
    int? ArrivalDelay,
    int? DepartureDelay,
    string Text,
    string Result,
    IReadOnlyList<ArchiveDetailDto> Details);
