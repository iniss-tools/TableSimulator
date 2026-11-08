namespace TableSimulator.Core.Archive;

/// <summary>Denny archiv najdeny v priecinkoch logov INISSu.</summary>
/// <param name="Kind">druh</param>
/// <param name="Date">den</param>
/// <param name="Path">subor</param>
/// <param name="Modified">posledna zmena (miestny cas)</param>
/// <param name="Length">velkost v bajtoch</param>
public sealed record ArchiveDay(ArchiveKind Kind, DateOnly Date, string Path, DateTime Modified, long Length);
