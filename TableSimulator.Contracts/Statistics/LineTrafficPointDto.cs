namespace TableSimulator.Contracts.Statistics;

/// <summary>Komunikacia linky v jednom intervale grafu.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Time">zaciatok intervalu</param>
/// <param name="Ok">spravy od INISSu v poriadku</param>
/// <param name="BadChecksum">spravy so zlym suctom</param>
/// <param name="NoBoard">spravy na adresu bez tabule</param>
/// <param name="Noise">useky bajtov mimo sprav (sum)</param>
/// <param name="Other">ine chybne spravy</param>
/// <param name="Replies">odpovede tabul bez poruchy</param>
/// <param name="Faults">odpovede ovplyvnene predstieranou poruchou (aj vynechane)</param>
/// <param name="BytesIn">bajty od INISSu</param>
/// <param name="BytesOut">bajty INISSu</param>
/// <param name="Connections">pripojenia INISSu</param>
/// <param name="Disconnections">odpojenia INISSu</param>
public sealed record LineTrafficPointDto(
    int Line,
    DateTimeOffset Time,
    int Ok,
    int BadChecksum,
    int NoBoard,
    int Noise,
    int Other,
    int Replies,
    int Faults,
    long BytesIn,
    long BytesOut,
    int Connections,
    int Disconnections);
