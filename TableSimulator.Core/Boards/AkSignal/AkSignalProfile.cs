namespace TableSimulator.Core.Boards.AkSignal;

/// <summary>
/// Ustredna AK Signal: co o nej vedia data stanice (svetla z Lights.xml, audio linky z Audio.xml) a co INISS2 neurcuje -
/// snimac tmy, stavy ustredne a casovanie. Patri do profilu tabule (<see cref="BoardProfile.AkSignal" />).
/// </summary>
public sealed record AkSignalProfile
{
    /// <summary>Predvolene nastavenie ustredne bez dat stanice.</summary>
    public static AkSignalProfile Default { get; } = new();

    /// <summary>Svetla, ktore ustredna riadi.</summary>
    public IReadOnlyList<LightCircuit> Lights { get; init; } = [];

    /// <summary>Audio linky servera, ktore cez ustrednu hlasia.</summary>
    public IReadOnlyList<AudioCircuit> Audio { get; init; } = [];

    /// <summary>Snimac tmy.</summary>
    public TwilightSensor Sensor { get; init; } = new();

    /// <summary>Stavy ustredne, ktore nastavuje obsluha simulatora (poruchy, vyssia priorita).</summary>
    public AkSignalConditions Conditions { get; init; } = new();

    /// <summary>Dlzka jedneho zvuku hlasenia AKSIGNALUH [s], ked banka zvukov nie je zname.</summary>
    public double SoundSeconds { get; init; } = 1.5;

    /// <summary>Ako dlho ustredna AKSIGNALUH po prikaze <c>sync</c> hlasi nezosuladenu banku [s].</summary>
    public int SyncSeconds { get; init; } = 2;

    /// <summary>Po kolkych sekundach bez dotazu servera ustredna hlasi, ze sa server nepyta.</summary>
    public int SilenceSeconds { get; init; } = 10;
}
