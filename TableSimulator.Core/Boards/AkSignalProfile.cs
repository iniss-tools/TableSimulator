using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards;

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

/// <summary>
/// Svetlo z Lights.xml - nazov, stanica a casy svietenia podla vlakov (na zobrazenie, svietenie riadi server).
/// </summary>
/// <param name="Name">nazov svetla (<c>jm</c>)</param>
/// <param name="Station">cislo stanice (<c>st</c>)</param>
public sealed record LightCircuit(string Name, int Station)
{
    /// <summary>Minut pred planovanym prichodom (<c>ppp</c>).</summary>
    public int BeforeArrival { get; init; }

    /// <summary>Minut pred planovanym odchodom z vychodzej stanice (<c>ppo</c>).</summary>
    public int BeforeDeparture { get; init; }

    /// <summary>Minut po skutocnom odchode (<c>pso</c>).</summary>
    public int AfterDeparture { get; init; }

    /// <summary>Minut po skutocnom prichode do cielovej stanice (<c>psp</c>).</summary>
    public int AfterArrival { get; init; }

    /// <summary>Kratsie zhasnutie sa nevykona [min] (<c>zh</c>).</summary>
    public int MinOff { get; init; }
}

/// <summary>
/// Audio linka servera z Audio.xml, ktora hlasi cez ustrednu.
/// </summary>
/// <param name="Name">nazov audio linky (<c>jm</c>)</param>
/// <param name="Station">cislo stanice (<c>st</c>)</param>
public sealed record AudioCircuit(string Name, int Station)
{
    /// <summary>Kanal ustredne AKSIGNALUH 1 alebo 2 (<c>ch</c>); pri AKSIGNAL sa neberie do uvahy.</summary>
    public int Channel { get; init; } = 1;
}

/// <summary>
/// Ako snimac tmy rozhoduje, ci je tma.
/// </summary>
public enum TwilightMode
{
    /// <summary>Podla vychodu a zapadu slnka v polohe simulatora.</summary>
    [JsonStringEnumMemberName("sun")] Sun,

    /// <summary>Stale tma.</summary>
    [JsonStringEnumMemberName("dark")] Dark,

    /// <summary>Stale svetlo.</summary>
    [JsonStringEnumMemberName("light")] Light
}

/// <summary>
/// Snimac tmy ustredne.
/// </summary>
public sealed record TwilightSensor
{
    /// <summary>Ako snimac rozhoduje.</summary>
    public TwilightMode Mode { get; init; } = TwilightMode.Sun;

    /// <summary>
    /// Posun voci vychodu a zapadu [min]: tma zacne tolko minut po zapade a skonci tolko minut pred vychodom; zaporny
    /// posun tmu predlzi.
    /// </summary>
    public int OffsetMinutes { get; init; }
}

/// <summary>
/// Stavy ustredne, ktore INISS2 cita, ale nemeni - nastavuje ich obsluha simulatora alebo scenar.
/// </summary>
public sealed record AkSignalConditions
{
    /// <summary>Bez poruchy.</summary>
    public static AkSignalConditions None { get; } = new();

    /// <summary>Porucha svetiel - chyba bit <c>0x08</c> / kluc <c>ok</c>.</summary>
    public bool LightsFault { get; init; }

    /// <summary>Bezi hlasenie s vyssou prioritou (napr. miestny mikrofon) - <c>0x02</c> / <c>hiprt</c>; rusi hlasenie servera.</summary>
    public bool HighPriority { get; init; }

    /// <summary>Chyba vykonu na vystupe - <c>0x08</c> zvuku / <c>pwr</c>.</summary>
    public bool OutputPowerError { get; init; }

    /// <summary>Chyba napajanie zosilnovaca - bez <c>0x10</c> zvuku / <c>sup</c>.</summary>
    public bool AmpSupplyMissing { get; init; }

    /// <summary>Chyba komunikacie so zosilnovacom - <c>comerr</c> (len AKSIGNALUH).</summary>
    public bool AmpCommError { get; init; }

    /// <summary>Banka zvukov nie je zosuladena - <c>sync</c> stale (len AKSIGNALUH).</summary>
    public bool BankNotSynced { get; init; }

    /// <summary>Aspon jeden stav.</summary>
    public bool Any => LightsFault || HighPriority || OutputPowerError || AmpSupplyMissing || AmpCommError || BankNotSynced;

    /// <summary>Stavy z tychto aj <paramref name="other" /> (kazdy plati, ak plati v jednom z nich).</summary>
    public AkSignalConditions With(AkSignalConditions other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new AkSignalConditions
        {
            LightsFault = LightsFault || other.LightsFault,
            HighPriority = HighPriority || other.HighPriority,
            OutputPowerError = OutputPowerError || other.OutputPowerError,
            AmpSupplyMissing = AmpSupplyMissing || other.AmpSupplyMissing,
            AmpCommError = AmpCommError || other.AmpCommError,
            BankNotSynced = BankNotSynced || other.BankNotSynced
        };
    }
}

/// <summary>
/// Co scenar docasne nastavuje ustredni nad jej profil: stavy sa pridaju k stavom z profilu (porucha zo scenara
/// plati aj bez poruchy v profile) a snimac sa prepne do daneho rezimu. Neuklada sa - po skonceni scenara plati profil.
/// </summary>
/// <param name="Conditions">stavy pridane k stavom z profilu</param>
/// <param name="Sensor">rezim snimaca tmy; null = podla profilu</param>
public sealed record AkSignalOverride(AkSignalConditions Conditions, TwilightMode? Sensor)
{
    /// <summary>Profil ustredne so stavmi a snimacom zo scenara.</summary>
    public AkSignalProfile Apply(AkSignalProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile with
        {
            Conditions = profile.Conditions.With(Conditions),
            Sensor = Sensor is { } mode ? profile.Sensor with { Mode = mode } : profile.Sensor
        };
    }

    /// <summary>Spoji dve prekrytia: stavy z oboch, snimac z neskorsieho, ak ho urcuje.</summary>
    public AkSignalOverride Then(AkSignalOverride later)
    {
        ArgumentNullException.ThrowIfNull(later);
        return new AkSignalOverride(Conditions.With(later.Conditions), later.Sensor ?? Sensor);
    }
}
