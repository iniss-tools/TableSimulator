namespace TableSimulator.Contracts.Station;

/// <summary>Svetlo nastupista z Lights.xml servera INISS2, ktore riadi ustredna AK Signal.</summary>
/// <param name="Name">nazov svetla (<c>jm</c>)</param>
/// <param name="Station">cislo stanice (<c>st</c>)</param>
public sealed record StationLightDto(string Name, int Station)
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
