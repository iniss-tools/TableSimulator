namespace TableSimulator.Application.Virtual;

/// <summary>Vlastne rozlozenie virtualnej tabule ulozene v databaze.</summary>
/// <param name="Id">kluc rozlozenia</param>
/// <param name="Name">nazov</param>
/// <param name="Definition">rozlozenie ako JSON (<see cref="VirtualLayouts.Serialize" />)</param>
public sealed record VirtualLayoutRecord(string Id, string Name, string Definition);
