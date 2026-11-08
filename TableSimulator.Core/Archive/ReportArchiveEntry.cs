namespace TableSimulator.Core.Archive;

/// <summary>Zaznam archivu hlaseni - hlasenie, zmena stavu vlaku, prihlasenie obsluhy a ine udalosti.</summary>
/// <param name="Time">cas zapisu</param>
/// <param name="Kind">druh udalosti (<c>u</c>) - text INISSu v jazyku instalacie (hlásenie, stav, prihlásenie…)</param>
/// <param name="Name">nazov hlasenia alebo udalosti (<c>n</c>)</param>
/// <param name="Auto">spustila ho automatika (<c>a</c>)</param>
/// <param name="Station">stanica</param>
/// <param name="Train">vlak; null, ak sa udalost vlaku netyka</param>
/// <param name="ArrivalTrack">kolaj prichodu (<c>kp</c>)</param>
/// <param name="DepartureTrack">kolaj odchodu (<c>ko</c>), ak sa lisi</param>
/// <param name="ArrivalDelay">posun prichodu (predpokladane meskanie) v minutach (<c>zp</c>)</param>
/// <param name="DepartureDelay">posun odchodu v minutach (<c>zo</c>), ak sa lisi</param>
/// <param name="Text">text hlasenia (<c>x</c>)</param>
/// <param name="Result">vysledok (<c>e</c>): <c>OK</c>, <c>break</c> (prerusene), <c>deleted</c>…</param>
/// <param name="Details">ostatne atributy (vyluka a odklon s textom z legendy, meskanie <c>zcp</c>/<c>zco</c>, priznaky…)</param>
public sealed record ReportArchiveEntry(
    TimeOnly Time,
    string Kind,
    string Name,
    bool Auto,
    string Station,
    ArchiveTrain? Train,
    string ArrivalTrack,
    string DepartureTrack,
    int? ArrivalDelay,
    int? DepartureDelay,
    string Text,
    string Result,
    IReadOnlyList<KeyValuePair<string, string>> Details);
