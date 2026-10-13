using System.ComponentModel;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Mcp;

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

/// <summary>Co ukazovala tabula v zazname.</summary>
public sealed record RecordingBoardResult(
    string Id,
    string Name,
    [property: Description("Length of the recording [s].")]
    double Duration,
    [property: Description("States of the board: time [s from the start of the recording] when it changed, settled = time of the last change within the state, rows from the top.")]
    IReadOnlyList<BoardStateDto> States,
    [property: Description("More states than the limit; the later ones were left out.")]
    bool Truncated);
