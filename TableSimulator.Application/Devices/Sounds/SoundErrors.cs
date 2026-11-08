using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Devices.Sounds;

/// <summary>Chyby nahravok banky zvukov.</summary>
public static class SoundErrors
{
    public static readonly Error NoBank = Error.NotFound("sound.noBank", "The INISS program in the settings has no sound bank.");

    public static Error NotFound(string name) => Error.NotFound("sound.notFound", $"The sound bank has no recording '{name}'.");

    public static Error Unreadable(string message) => Error.Problem("sound.unreadable", $"The recording cannot be read: {message}");
}
