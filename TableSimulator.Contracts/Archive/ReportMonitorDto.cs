namespace TableSimulator.Contracts.Archive;

/// <summary>
/// Monitor hlaseni (GET /api/archive/reports/live): najnovsi denny archiv hlaseni v priecinkoch logov INISSu, zaznamy
/// od najnovsieho. Dalsie prinasa WebSocket (sprava <c>reports</c>).
/// </summary>
/// <param name="Date">den sledovaneho archivu; null = v priecinkoch ziadny archiv hlaseni nie je</param>
/// <param name="File">meno suboru</param>
/// <param name="Entries">najnovsie zaznamy (najviac <c>limit</c>) od najnovsieho</param>
/// <param name="Total">pocet zaznamov dna</param>
/// <param name="Skipped">riadky, ktore sa nedali precitat</param>
public sealed record ReportMonitorDto(DateOnly? Date, string? File, IReadOnlyList<ReportArchiveEntryDto> Entries, int Total, int Skipped);
