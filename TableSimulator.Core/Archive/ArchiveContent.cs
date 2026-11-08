namespace TableSimulator.Core.Archive;

/// <summary>Precitany denny archiv.</summary>
/// <param name="Tables">zaznamy archivu tabul (pri archive hlaseni prazdne)</param>
/// <param name="Reports">zaznamy archivu hlaseni (pri archive tabul prazdne)</param>
/// <param name="Skipped">riadky, ktore sa nedali precitat (poskodene alebo prave zapisovane)</param>
public sealed record ArchiveContent(IReadOnlyList<TableArchiveEntry> Tables, IReadOnlyList<ReportArchiveEntry> Reports, int Skipped);
