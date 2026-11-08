namespace TableSimulator.Core.Archive;

/// <summary>Zaznam archivu tabul - co bolo na tabuliach o vlaku (alebo informacny text) od casu zaznamu.</summary>
/// <param name="Time">cas zapisu</param>
/// <param name="Station">stanica (INISS 3.34 ju nezapisoval)</param>
/// <param name="Train">vlak; null pri informacnom texte</param>
/// <param name="Text">informacny text (<c>tx</c>); prazdny pri zazname vlaku alebo ked text zmizol</param>
/// <param name="Tables">tabule</param>
public sealed record TableArchiveEntry(TimeOnly Time, string Station, ArchiveTrain? Train, string Text, IReadOnlyList<TableArchiveTable> Tables);
