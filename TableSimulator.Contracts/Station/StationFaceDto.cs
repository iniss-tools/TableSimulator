namespace TableSimulator.Contracts.Station;

/// <summary>Co o cele tabule vedia data stanice - podla toho simulator novej tabuli zalozi predlohu cela a rozmer.</summary>
/// <param name="Kind">druh tabule</param>
/// <param name="LinesPerRecord">riadkov tabule na jeden zaznam</param>
/// <param name="Records">zaznamov na tabuli</param>
/// <param name="Width">sirka riadku v bodoch; 0 = neznama</param>
/// <param name="RowHeight">vyska riadku v bodoch z katalogu; 0 = neznama</param>
/// <param name="Columns">stlpce zobrazene pri vlaku</param>
/// <param name="Tracks">texty kolaji, nad ktorymi tabula stoji</param>
/// <param name="Platforms">nastupistia tychto kolaji</param>
/// <param name="Station">nazov stanice</param>
public sealed record StationFaceDto(
    StationBoardKind Kind,
    int LinesPerRecord,
    int Records,
    int Width,
    int RowHeight,
    IReadOnlyList<StationFaceColumnDto> Columns,
    IReadOnlyList<string> Tracks,
    IReadOnlyList<string> Platforms,
    string Station);
