using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Monitor;

/// <summary>
/// Predloha tabule - typ panelu z programu Monitor prevedeny na profil simulatora.
/// </summary>
/// <param name="File">subor typu panelu (.cfg)</param>
/// <param name="Name">nazov typu panelu</param>
/// <param name="Profile">profil tabule</param>
public sealed record BoardTemplate(string File, string Name, BoardProfile Profile);
