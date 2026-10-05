namespace TableSimulator.Contracts.Archive;

/// <summary>Zaznam archivu tabul - co bolo na tabuliach o vlaku, alebo informacny text.</summary>
/// <param name="Time">cas zapisu</param>
/// <param name="Station">stanica</param>
/// <param name="Train">vlak; null pri informacnom texte</param>
/// <param name="Text">informacny text</param>
/// <param name="Tables">tabule</param>
public sealed record TableArchiveEntryDto(TimeOnly Time, string Station, ArchiveTrainDto? Train, string Text, IReadOnlyList<TableArchiveTableDto> Tables);
