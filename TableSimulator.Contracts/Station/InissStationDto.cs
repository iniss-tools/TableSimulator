namespace TableSimulator.Contracts.Station;

/// <summary>Linky a tabule instalacie INISSu z nastavenia simulatora (nahlad pred nacitanim).</summary>
/// <param name="Program">program INISSu</param>
/// <param name="AppName">vetva registra</param>
/// <param name="Registry">nastavenie sa citalo aj z registra (inak len zo suboru .INI)</param>
/// <param name="Grafikons">grafikony, z ktorych sa citali tabule</param>
/// <param name="Lines">linky v poradi, v akom ich INISS zaklada</param>
/// <param name="Unserved">tabule s adresou, ktorym INISS nic neposle</param>
/// <param name="LogFolder">priecinok logov INISSu s XML tabul</param>
/// <param name="LogFolderExists">priecinok logov existuje</param>
/// <param name="XmlTables">tabule s exportom do XML (virtualne tabule)</param>
public sealed record InissStationDto(
    string Program,
    string AppName,
    bool Registry,
    IReadOnlyList<string> Grafikons,
    IReadOnlyList<InissLineDto> Lines,
    IReadOnlyList<InissTableDto> Unserved,
    string LogFolder,
    bool LogFolderExists,
    IReadOnlyList<StationXmlTableDto> XmlTables);
