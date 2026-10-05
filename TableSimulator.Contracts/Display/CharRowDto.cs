namespace TableSimulator.Contracts.Display;

/// <summary>Riadok znakovej tabule (Elektrocas) zhora od 0.</summary>
public sealed record CharRowDto(int Row, CharLayerDto[] Layers);
