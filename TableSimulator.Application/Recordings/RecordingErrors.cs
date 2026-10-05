using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Recordings;

/// <summary>Chyby a kody chyb zaznamov komunikacie.</summary>
public static class RecordingErrors
{
    public const string Name = "recording.name";
    public const string Note = "recording.note";
    public const string Empty = "recording.empty";
    public const string Speed = "replay.speed";

    public static Error NotFound(string name) => Error.NotFound("recording.notFound", $"Recording '{name}' does not exist.");

    public static Error InProgress(string name) => Error.Conflict("recording.inProgress", $"Recording '{name}' is being recorded.");

    public static Error Format() => Error.Problem("recording.format", "The file is not a simulator recording.");
}
