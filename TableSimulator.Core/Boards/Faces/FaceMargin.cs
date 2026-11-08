namespace TableSimulator.Core.Boards.Faces;

/// <summary>
/// Miesto okolo matice pre prvky cela (v bodoch).
/// </summary>
/// <param name="Left">vlavo</param>
/// <param name="Top">hore</param>
/// <param name="Right">vpravo</param>
/// <param name="Bottom">dole</param>
public sealed record FaceMargin(int Left = 0, int Top = 0, int Right = 0, int Bottom = 0);
