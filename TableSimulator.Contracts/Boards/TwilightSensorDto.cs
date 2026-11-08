namespace TableSimulator.Contracts.Boards;

/// <summary>Snimac tmy ustredne AK Signal.</summary>
/// <param name="Mode">ako snimac rozhoduje</param>
/// <param name="OffsetMinutes">posun voci vychodu a zapadu [min]: tma zacne tolko minut po zapade a skonci tolko pred vychodom</param>
public sealed record TwilightSensorDto(TwilightMode Mode = TwilightMode.Sun, int OffsetMinutes = 0);
