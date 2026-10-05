namespace TableSimulator.Contracts.Archive;

/// <summary>Dni, za ktore su v priecinkoch logov INISSu denne archivy (GET /api/archive), od najnovsieho.</summary>
/// <param name="Tables">dni archivu tabul (<c>TAB&lt;datum&gt;.XML</c>)</param>
/// <param name="Reports">dni archivu hlaseni (<c>REP&lt;datum&gt;.XML</c>)</param>
public sealed record ArchiveDaysDto(IReadOnlyList<DateOnly> Tables, IReadOnlyList<DateOnly> Reports);
