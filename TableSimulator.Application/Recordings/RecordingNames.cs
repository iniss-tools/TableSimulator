namespace TableSimulator.Application.Recordings;

/// <summary>Meno zaznamu je nazov suboru bez pripony - bez ciest a nepovolenych znakov.</summary>
public static class RecordingNames
{
    public static bool IsValid(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !name.Contains("..", StringComparison.Ordinal);
}
