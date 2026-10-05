namespace TableSimulator.Contracts.Boards;

/// <summary>Text hlavicky tabule; polohy a vysky v bodoch matice.</summary>
public sealed record HeaderTextDto(int X, int Y, int Height, string Text);
