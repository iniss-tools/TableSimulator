using System.ComponentModel;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Mcp.Results;

/// <summary>Porovnanie dvoch zaznamov pre model.</summary>
public sealed record ComparisonResult(
    RecordingSideDto A,
    RecordingSideDto B,
    [property: Description("Boards that showed the same sequence of states in both recordings.")]
    int Same,
    int Different,
    [property: Description("Boards present only in recording A.")]
    int OnlyA,
    [property: Description("Boards present only in recording B.")]
    int OnlyB,
    [property: Description("Compared boards (without the same ones when onlyDifferences); steps align the states of A and B: same, changed, removed (only in A), added (only in B).")]
    IReadOnlyList<BoardComparisonDto> Boards,
    [property: Description("Boards whose steps were cut to maxSteps.")]
    IReadOnlyList<string> TruncatedBoards);
