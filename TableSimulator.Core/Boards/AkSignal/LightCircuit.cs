namespace TableSimulator.Core.Boards.AkSignal;

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
