using TableSimulator.Contracts.Boards;

namespace TableSimulator.Contracts.Monitor;

/// <summary>Predloha tabule z typu panelu ELEN Monitora.</summary>
/// <param name="File">subor typu panelu (<c>.cfg</c>)</param>
public sealed record BoardTemplateDto(string File, string Name, BoardProfileDto Profile);
