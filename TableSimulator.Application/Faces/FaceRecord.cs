namespace TableSimulator.Application.Faces;

/// <summary>Vlastna predloha cela tabule ulozena v databaze.</summary>
/// <param name="Id">kluc predlohy</param>
/// <param name="Name">nazov</param>
/// <param name="Definition">predloha ako JSON (<see cref="BoardFaces.Serialize" />)</param>
public sealed record FaceRecord(string Id, string Name, string Definition);
