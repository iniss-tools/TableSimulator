using System.ComponentModel;
using TableSimulator.Contracts.Frames;

namespace TableSimulator.Mcp;

/// <summary>Vyber z logu komunikacie.</summary>
public sealed record FramesResult(
    IReadOnlyList<FrameDto> Frames,
    [property: Description("Sequence number of the newest message in the log; pass it as 'after' to get only newer messages.")]
    long Next,
    [property: Description("More messages matched than the limit; the older ones were left out.")]
    bool Truncated);
