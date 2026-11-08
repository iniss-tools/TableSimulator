namespace TableSimulator.Core.Boards.AkSignal;

/// <summary>
/// Zemepisna poloha, pre ktoru sa pocita vychod a zapad slnka.
/// </summary>
/// <param name="Latitude">sirka v stupnoch, sever kladne</param>
/// <param name="Longitude">dlzka v stupnoch, vychod kladne</param>
public readonly record struct GeoLocation(double Latitude, double Longitude)
{
    /// <summary>Stred Slovenska - predvolena poloha simulatora.</summary>
    public static GeoLocation Default { get; } = new(48.67, 19.70);

    /// <summary>Poloha je na Zemi.</summary>
    public bool IsValid => Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;
}
