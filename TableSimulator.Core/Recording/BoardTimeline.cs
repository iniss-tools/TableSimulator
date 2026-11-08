namespace TableSimulator.Core.Recording;

/// <summary>Zmeny textu jednej tabule v zazname.</summary>
/// <param name="Id">tabula <c>linka-adresa</c></param>
/// <param name="Name">nazov tabule v zazname</param>
/// <param name="States">stavy v poradi casu (kazdy sa lisi od predchadzajuceho)</param>
public sealed record BoardTimeline(string Id, string Name, IReadOnlyList<BoardState> States);
