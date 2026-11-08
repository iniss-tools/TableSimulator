namespace TableSimulator.Core.Boards.AkSignal;

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
