namespace TableSimulator.Contracts.Station;

/// <summary>
/// Linky a tabule stanice, ktore ma simulator mat (POST /api/station) - posiela ich GVDEditor pri presmerovani liniek
/// INISSu na simulator.
/// </summary>
public sealed record StationImportRequest(IReadOnlyList<StationLineDto> Lines, IReadOnlyList<StationBoardDto> Boards)
{
    /// <summary>Priecinok logov INISSu (<c>PathNames\LogPath</c>) - simulator v nom bude hladat XML tabul; null = nie.</summary>
    public string? LogFolder { get; init; }

    /// <summary>Tabule s exportom do XML - simulator pre ne zalozi virtualne tabule, ktore este nema.</summary>
    public IReadOnlyList<StationXmlTableDto> XmlTables { get; init; } = [];
}
