namespace TableSimulator.Contracts.Boards;

/// <summary>Hodiny v hlavicke tabule; polohy a vysky v bodoch matice.</summary>
public sealed record HeaderClockDto(int X, int Y, int Height);
