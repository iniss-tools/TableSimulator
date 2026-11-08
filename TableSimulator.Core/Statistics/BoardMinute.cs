namespace TableSimulator.Core.Statistics;

/// <summary>Komunikacia tabule za jednu minutu.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa tabule</param>
/// <param name="Minute">minuta od 1. 1. 1970 UTC</param>
/// <param name="Frames">spravy pre tabulu</param>
/// <param name="Acks">kladne odpovede</param>
/// <param name="Naks">zaporne odpovede (aj predstierane poruchy)</param>
/// <param name="Unanswered">spravy bez odpovede (porucha neodpoveda)</param>
/// <param name="MaxGapMs">najdlhsia pauza medzi spravami, ktora sa v tejto minute skoncila [ms]; null = ziadna</param>
public sealed record BoardMinute(int Line, int Address, long Minute, int Frames, int Acks, int Naks, int Unanswered, long? MaxGapMs);
