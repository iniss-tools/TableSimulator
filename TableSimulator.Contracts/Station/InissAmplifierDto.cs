namespace TableSimulator.Contracts.Station;

/// <summary>Spinacia jednotka zosilnovaca ELSVO zo zvukovych okruhov INISSu (Audio.txt).</summary>
/// <param name="Line">cislo linky (stlpec 8 E&lt;n&gt;)</param>
/// <param name="Address">adresa jednotky (stlpec 9)</param>
/// <param name="Name">okruhy, ktore jednotka spina</param>
/// <param name="Shown">simulator ju na linke zobrazi (linka triedy ELSVO)</param>
public sealed record InissAmplifierDto(int Line, int Address, string Name, bool Shown);
