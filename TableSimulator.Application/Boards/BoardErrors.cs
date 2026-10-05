using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Boards;

/// <summary>Chyby a kody chyb tabul.</summary>
public static class BoardErrors
{
    public const string Line = "board.line";
    public const string Address = "board.address";
    public const string SecondAddress = "board.secondAddress";
    public const string Manufacturer = "board.manufacturer";
    public const string Width = "board.width";
    public const string Rows = "board.rows";
    public const string RowHeight = "board.rowHeight";
    public const string Alternation = "board.alternation";
    public const string Blink = "board.blink";
    public const string ScrollSpeed = "board.scrollSpeed";
    public const string HeaderHeight = "board.headerHeight";
    public const string FaultKind = "fault.kind";
    public const string FaultDelay = "fault.delay";
    public const string FaultStatus = "fault.status";

    public static Error NotFound(string id) => Error.NotFound("board.notFound", $"Board {id} does not exist.");

    public static Error Invalid(string message) => Error.Problem("board.invalid", message);
}
