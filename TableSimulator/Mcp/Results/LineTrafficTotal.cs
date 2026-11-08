using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Sucty linky za obdobie.</summary>
public sealed record LineTrafficTotal(
    int Line,
    [property: Description("Messages from INISS processed normally.")]
    int Ok,
    int BadChecksum,
    [property: Description("Messages for an address without a board.")]
    int NoBoard,
    [property: Description("Runs of bytes outside messages.")]
    int Noise,
    [property: Description("Other invalid messages.")]
    int Other,
    [property: Description("Board replies without a simulated fault.")]
    int Replies,
    [property: Description("Replies affected by a simulated fault (also omitted ones).")]
    int Faults,
    [property: Description("Bytes from INISS.")]
    long BytesIn,
    [property: Description("Bytes to INISS.")]
    long BytesOut,
    [property: Description("INISS connected to the line.")]
    int Connections,
    [property: Description("INISS disconnected from the line.")]
    int Disconnections);
