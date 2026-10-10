using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Recordings;

/// <summary>Chyby a kody chyb zaznamov komunikacie.</summary>
public static class RecordingErrors
{
    public const string Name = "recording.name";
    public const string Note = "recording.note";
    public const string Empty = "recording.empty";
    public const string Speed = "replay.speed";
    public const string Time = "recording.time";
    public const string Buckets = "recording.buckets";

    public static Error NoReplay() => Error.Conflict("replay.none", "No recording has been played yet.");

    public static Error NotFound(string name) => Error.NotFound("recording.notFound", $"Recording '{name}' does not exist.");

    public static Error InProgress(string name) => Error.Conflict("recording.inProgress", $"Recording '{name}' is being recorded.");

    public static Error BoardNotFound(string board) => Error.NotFound("recording.boardNotFound", $"Board '{board}' is not in the recording.");

    public static Error Format() => Error.Problem("recording.format", "The file is not a simulator recording.");
}
