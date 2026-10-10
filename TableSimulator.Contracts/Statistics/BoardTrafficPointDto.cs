namespace TableSimulator.Contracts.Statistics;

/// <summary>Komunikacia tabule v jednom intervale grafu.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa tabule</param>
/// <param name="Time">zaciatok intervalu</param>
/// <param name="Frames">spravy pre tabulu</param>
/// <param name="Acks">kladne odpovede</param>
/// <param name="Naks">zaporne odpovede (aj predstierane poruchy)</param>
/// <param name="Unanswered">spravy bez odpovede</param>
/// <param name="MaxGapMs">najdlhsia pauza medzi spravami, ktora v intervale skoncila [ms]; null = ziadna</param>
public sealed record BoardTrafficPointDto(int Line, int Address, DateTimeOffset Time, int Frames, int Acks, int Naks, int Unanswered, long? MaxGapMs);
