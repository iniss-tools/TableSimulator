using TableSimulator.Contracts.Station;

namespace TableSimulator.Contracts.Boards;

/// <summary>Ustredna AK Signal: svetla a audio linky z dat stanice, snimac tmy, stavy a casovanie.</summary>
public sealed record AkSignalProfileDto
{
    /// <summary>Svetla, ktore ustredna riadi (z Lights.xml servera).</summary>
    public IReadOnlyList<StationLightDto> Lights { get; init; } = [];

    /// <summary>Audio linky servera, ktore cez ustrednu hlasia (z Audio.xml).</summary>
    public IReadOnlyList<StationAudioDto> Audio { get; init; } = [];

    /// <summary>Snimac tmy.</summary>
    public TwilightSensorDto Sensor { get; init; } = new();

    /// <summary>Stavy ustredne.</summary>
    public AkSignalConditionsDto Conditions { get; init; } = new();

    /// <summary>Dlzka jedneho zvuku hlasenia AKSIGNALUH [s], ked banka zvukov nie je znama.</summary>
    public double SoundSeconds { get; init; } = 1.5;

    /// <summary>Ako dlho ustredna AKSIGNALUH po <c>sync</c> hlasi nezosuladenu banku [s].</summary>
    public int SyncSeconds { get; init; } = 2;

    /// <summary>Po kolkych sekundach bez dotazu servera ustredna hlasi, ze sa server nepyta.</summary>
    public int SilenceSeconds { get; init; } = 10;
}
