using TableSimulator.Contracts.Boards;

namespace TableSimulator.Contracts.Live;

/// <summary>Zmenila sa tabula.</summary>
public sealed record BoardMessage(BoardDto Board) : LiveMessage;
