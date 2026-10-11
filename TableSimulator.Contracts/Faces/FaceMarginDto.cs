namespace TableSimulator.Contracts.Faces;

/// <summary>Miesto okolo matice pre prvky cela (v bodoch).</summary>
public sealed record FaceMarginDto(int Left = 0, int Top = 0, int Right = 0, int Bottom = 0);
