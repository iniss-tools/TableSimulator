using System.ComponentModel;
using TableSimulator.Contracts.Lines;

namespace TableSimulator.Mcp.Results;

/// <summary>Linka v prehlade.</summary>
public sealed record LineSummary(
    int Number,
    string Name,
    [property: Description("INISS table class of the line, selects the protocol (e.g. 4 = ELEN, 1 = Elektrocas LCD, 128 = remote INISS).")]
    int TableClass,
    [property: Description("Where INISS connects: host:port, pipe name or serial port.")]
    string Endpoint,
    LineStatus Status,
    string? StatusDetail,
    DateTimeOffset StatusSince,
    [property: Description("When the last message from INISS arrived; null = none yet.")]
    DateTimeOffset? LastReceivedAt,
    [property: Description("Number of boards on the line.")]
    int Boards,
    long Frames,
    long BadFrames);
