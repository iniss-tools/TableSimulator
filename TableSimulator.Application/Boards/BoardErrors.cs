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
    public const string Rotation = "board.rotation";
    public const string Flaps = "board.flaps";
    public const string CharFonts = "board.charFonts";
    public const string AkSignal = "board.akSignal";
    public const string FaultKind = "fault.kind";
    public const string FaultDelay = "fault.delay";
    public const string FaultStatus = "fault.status";

    public static Error NotFound(string id) => Error.NotFound("board.notFound", $"Board {id} does not exist.");

    public static Error NotAkSignal(string id) => Error.Problem("board.notAkSignal", $"Board {id} is not an AK Signal controller.");

    public static Error NotRemoteAudio(string id) => Error.Problem("board.notRemoteAudio", $"Board {id} is not the public address of a remote INISS.");

    public static Error NothingPlaying(string id) => Error.Problem("board.nothingPlaying", $"Nothing is playing on {id}.");

    public static Error Invalid(string message) => Error.Problem("board.invalid", message);
}
