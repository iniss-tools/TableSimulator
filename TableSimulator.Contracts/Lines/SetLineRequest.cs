namespace TableSimulator.Contracts.Lines;

/// <summary>Nastavenie linky (PUT /api/lines/{number}).</summary>
public sealed record SetLineRequest
{
    /// <summary>Cislo linky 1-100; pri PUT plati cislo z adresy.</summary>
    public int Number { get; init; }

    /// <summary>Trieda linky (TableClass).</summary>
    public int TableClass { get; init; }

    /// <summary>Kde linka pocuva: <c>TCP://host:port</c>, <c>UDP://…</c>, <c>\\.\PIPE\meno</c>, <c>COMn:rychlost,parita,data,stop</c>.</summary>
    public string Endpoint { get; init; } = "";

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; init; } = "";

    /// <summary>Rezim.</summary>
    public LineMode Mode { get; init; }

    /// <summary>Samoucenie - na spravu pre neznamu adresu vytvorit tabulu.</summary>
    public bool AutoLearn { get; init; } = true;

    /// <summary>Vyrobca tabul zo samoucenia.</summary>
    public string Manufacturer { get; init; } = "ELEN";

    /// <summary>Seriovy port zrkadla (<c>COM10:9600,n,8,1</c>); null = bez zrkadla.</summary>
    public string? Mirror { get; init; }
}
