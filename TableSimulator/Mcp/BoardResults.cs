using System.ComponentModel;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Display;

namespace TableSimulator.Mcp;

/// <summary>Tabule s textom.</summary>
public sealed record BoardsResult(IReadOnlyList<BoardSummary> Boards);

/// <summary>Tabula v zozname.</summary>
public sealed record BoardSummary(
    [property: Description("Board id \"line-address\".")]
    string Id,
    string Name,
    int Line,
    int Address,
    string Manufacturer,
    [property: Description("elen, chars, flaps, amplifier or unknown.")]
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

/// <summary>Podrobnosti tabule.</summary>
public sealed record BoardDetail(
    string Id,
    string Name,
    int Line,
    int Address,
    [property: Description("Second address of a two-sided board; null = only one.")]
    int? SecondAddress,
    string Manufacturer,
    [property: Description("elen, chars, flaps, amplifier or unknown.")]
    string Family,
    [property: Description("Design set for the board; null = default for the manufacturer.")]
    string? Design,
    [property: Description("Design actually used: the board's design, or the default design of the manufacturer when none is set or it does not exist.")]
    string DesignInUse,
    [property: Description("Created automatically by the first message to an unknown address.")]
    bool AutoCreated,
    [property: Description("Simulated power outage: the board is dark and does not answer.")]
    bool PoweredOff,
    [property: Description("Simulated fault: kind, reply delay [ms] for delay, status bits for status.")]
    BoardFaultDto Fault,
    [property: Description("INISS sends messages meant for another manufacturer; null = fine.")]
    ManufacturerMismatchDto? Mismatch,
    [property: Description("Message counters: frames from INISS, acknowledged (acks), rejected (naks), unanswered.")]
    BoardStatsDto Stats,
    [property: Description("Brightness set by INISS [%].")]
    int Brightness,
    [property: Description("Clock time set by INISS; null = not set.")]
    DateTime? Clock,
    [property: Description("Increases with every change of the content or settings; pass it to wait_for_board_change.")]
    long Version,
    [property: Description(BoardTexts.RowsDescription)]
    IReadOnlyList<string> Rows,
    [property: Description("Board settings (size, fonts, timing); only with raw = true.")]
    BoardProfileDto? Profile,
    [property: Description("Raw content with fonts, character codes and positions; only with raw = true.")]
    DisplayDto? Display);

/// <summary>Spolocne popisy textu tabule.</summary>
public static class BoardTexts
{
    public const string RowsDescription =
        "What the board shows, rows from the top; an empty row is \"\", alternating texts are {a|b}, the moving text of an elen board is the last row \"» text\", "
        + "split-flap modules are joined with \" | \", an amplifier is \"ON 1\", \"ON 2\" or \"OFF\".";
}
