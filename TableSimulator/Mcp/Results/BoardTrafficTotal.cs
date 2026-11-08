using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Sucty tabule za obdobie.</summary>
public sealed record BoardTrafficTotal(
    string Id,
    int Line,
    int Address,
    [property: Description("Messages for the board.")]
    int Frames,
    [property: Description("Positive replies.")]
    int Acks,
    [property: Description("Negative replies (also simulated faults).")]
    int Naks,
    [property: Description("Messages without a reply.")]
    int Unanswered,
    [property: Description("Longest gap between two messages for the board [ms]; null = none.")]
    long? MaxGapMs);
