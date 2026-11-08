using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Rozumne medze nastavenia ustredne AK Signal.</summary>
public static class AkSignalRules
{
    /// <summary>Popis chyby pre klienta.</summary>
    public const string Message =
        "The sensor offset must be -240 to 240 min, a sound 0.1-60 s, the bank sync 0-600 s and the server silence 1-3600 s.";

    /// <summary>Snimac a casovanie su v medziach; null = hodnota sa nemeni.</summary>
    public static bool IsValid(TwilightSensorDto? sensor, double? soundSeconds, int? syncSeconds, int? silenceSeconds) =>
        sensor is { OffsetMinutes: >= -240 and <= 240 } && Enum.IsDefined(sensor.Mode)
        && soundSeconds is null or (>= 0.1 and <= 60)
        && syncSeconds is null or (>= 0 and <= 600)
        && silenceSeconds is null or (>= 1 and <= 3600);
}
