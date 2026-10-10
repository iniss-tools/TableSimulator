using TableSimulator.Contracts.Archive;

namespace TableSimulator.Contracts.Live;

/// <summary>
/// Pribudli hlasenia v sledovanom archive (monitor hlaseni). Pri <paramref name="Reset" /> sa sleduje iny subor (novy
/// den) - zoznam treba nacitat znova, <paramref name="Added" /> je vtedy prazdne.
/// </summary>
/// <param name="Date">den sledovaneho archivu</param>
/// <param name="Added">nove zaznamy v poradi suboru (od najstarsieho)</param>
/// <param name="Reset">zoznam nacitat znova</param>
/// <param name="Total">pocet zaznamov dna</param>
/// <param name="Skipped">riadky, ktore sa nedali precitat</param>
public sealed record ReportsMessage(DateOnly? Date, IReadOnlyList<ReportArchiveEntryDto> Added, bool Reset, int Total, int Skipped) : LiveMessage;
