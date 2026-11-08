using System.ComponentModel;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Mcp.Results;

/// <summary>Tabula v zozname.</summary>
public sealed record BoardSummary(
    [property: Description("Board id \"line-address\".")]
    string Id,
    string Name,
    int Line,
    int Address,
    string Manufacturer,
    [property: Description("elen, chars, flaps, amplifier, aksignal, remoteaudio or unknown.")]
    string Family,
    [property: Description("Increases with every change of the content or settings; pass it to wait_for_board_change.")]
    long Version,
    [property: Description("Simulated power outage: the board is dark and does not answer.")]
    bool PoweredOff,
    FaultKind Fault,
    [property: Description("When the last message for this board arrived; null = none yet.")]
    DateTimeOffset? LastFrameAt,
    [property: Description(BoardTexts.RowsDescription + " Null when the text was not requested.")]
    IReadOnlyList<string>? Rows);
