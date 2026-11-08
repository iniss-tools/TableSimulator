namespace TableSimulator.Contracts.AkSignal;

/// <summary>Zaznam logu svetiel servera INISS2 - preco server svetlo zapol alebo vypol.</summary>
/// <param name="At">cas zapisu</param>
/// <param name="Station">nazov stanice z logu</param>
/// <param name="Name">nazov svetla</param>
/// <param name="Event">udalost</param>
public sealed record ServerLightEventDto(DateTimeOffset At, string Station, string Name, ServerLightEvent Event)
{
    /// <summary>Ustredna simulatora so svetlom tohto nazvu; null = ziadna.</summary>
    public string? BoardId { get; init; }

    /// <summary>Rucna zmena obsluhou.</summary>
    public bool Manual { get; init; }

    /// <summary>Rezim (<c>OFF</c>, <c>AUTO</c>, <c>FORCELIGHT</c>); prazdny = nezadany.</summary>
    public string Mode { get; init; } = "";

    /// <summary>Vlak potrebuje svetlo.</summary>
    public bool TrainNeedsLight { get; init; }

    /// <summary>Priznaky stavu svetla (<c>LIGHTON</c>, <c>TWILIGHTON</c>, <c>NOTCONNECTED</c>…).</summary>
    public IReadOnlyList<string> State { get; init; } = [];

    /// <summary>Kedy sa svetlo najskor rozsvieti pre dalsi vlak.</summary>
    public DateTimeOffset? TurnOn { get; init; }

    /// <summary>Kedy svetlo zhasne po vlaku, pre ktory svieti.</summary>
    public DateTimeOffset? TurnOff { get; init; }

    /// <summary>Cislo vlaku, pre ktory svetlo svieti; prazdne = ziadny.</summary>
    public string Train { get; init; } = "";
}
