namespace TableSimulator.Contracts.Archive;

/// <summary>Stranka archivu hlaseni (GET /api/archive/reports/{datum}).</summary>
/// <param name="Date">den</param>
/// <param name="Entries">vybrane zaznamy</param>
/// <param name="Total">pocet vyhovujucich zaznamov dna</param>
/// <param name="Skipped">riadky suboru, ktore sa nedali precitat (aj textovy tvar logu hlaseni)</param>
public sealed record ReportArchivePageDto(DateOnly Date, IReadOnlyList<ReportArchiveEntryDto> Entries, int Total, int Skipped);
