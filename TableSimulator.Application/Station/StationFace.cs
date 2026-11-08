using TableSimulator.Core.Boards.Faces;

namespace TableSimulator.Application.Station;

/// <summary>Celo tabule z dat stanice: predloha (bez kluca), napoveda kluca a hodnoty premennych pre tabulu.</summary>
/// <param name="Face">predloha s nazvom, bez kluca</param>
/// <param name="IdHint">napoveda kluca predlohy (druh tabule a stanica)</param>
/// <param name="Values">hodnoty premennych cela pre tabulu</param>
public sealed record StationFace(BoardFace Face, string IdHint, IReadOnlyDictionary<string, string> Values);
