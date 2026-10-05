namespace TableSimulator.Contracts.Archive;

/// <summary>Stranka archivu tabul (GET /api/archive/tables/{datum}).</summary>
/// <param name="Date">den</param>
/// <param name="Entries">vybrane zaznamy</param>
/// <param name="Total">pocet vyhovujucich zaznamov dna</param>
/// <param name="Skipped">riadky suboru, ktore sa nedali precitat</param>
public sealed record TableArchivePageDto(DateOnly Date, IReadOnlyList<TableArchiveEntryDto> Entries, int Total, int Skipped);
