namespace TableSimulator.Contracts.Settings;

/// <summary>Zemepisna poloha pre vychod a zapad slnka (snimac tmy ustredni AK Signal).</summary>
/// <param name="Latitude">sirka v stupnoch, sever kladne</param>
/// <param name="Longitude">dlzka v stupnoch, vychod kladne</param>
public sealed record GeoLocationDto(double Latitude, double Longitude);
