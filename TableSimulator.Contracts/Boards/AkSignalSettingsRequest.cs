namespace TableSimulator.Contracts.Boards;

/// <summary>
/// Snimac, stavy a casovanie ustredne AK Signal (PUT /api/boards/{id}/aksignal) - svetla a audio linky su data stanice
/// a ostanu.
/// </summary>
/// <param name="Sensor">snimac tmy</param>
/// <param name="Conditions">stavy ustredne</param>
public sealed record AkSignalSettingsRequest(TwilightSensorDto Sensor, AkSignalConditionsDto Conditions)
{
    /// <summary>Dlzka jedneho zvuku hlasenia AKSIGNALUH [s]; null = nechat.</summary>
    public double? SoundSeconds { get; init; }

    /// <summary>Ako dlho ustredna po <c>sync</c> hlasi nezosuladenu banku [s]; null = nechat.</summary>
    public int? SyncSeconds { get; init; }

    /// <summary>Po kolkych sekundach bez dotazu servera ustredna hlasi, ze sa server nepyta; null = nechat.</summary>
    public int? SilenceSeconds { get; init; }
}
