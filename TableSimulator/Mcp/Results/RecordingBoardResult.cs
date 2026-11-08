using System.ComponentModel;
using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Mcp.Results;

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
