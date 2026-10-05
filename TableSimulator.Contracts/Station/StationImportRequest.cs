namespace TableSimulator.Contracts.Station;

/// <summary>
/// Linky a tabule stanice, ktore ma simulator mat (POST /api/station) - posiela ich GVDEditor pri presmerovani liniek
/// INISSu na simulator.
/// </summary>
public sealed record StationImportRequest(IReadOnlyList<StationLineDto> Lines, IReadOnlyList<StationBoardDto> Boards);
