using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;

namespace TableSimulator.Persistence.Simulation;

/// <summary>Linka simulatora v databaze (tabulka <c>Lines</c>).</summary>
public sealed class LineRecord
{
    /// <summary>Cislo linky 1-100.</summary>
    public int Number { get; set; }

    public int TableClass { get; set; }

    /// <summary>Kde linka pocuva (<c>TCP://host:port</c>, rura, COM).</summary>
    public string Endpoint { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Rezim - v databaze kluc <c>display</c>/<c>monitor</c>.</summary>
    public LineMode Mode { get; set; }

    public bool AutoLearn { get; set; }

    public string Manufacturer { get; set; } = "";

    /// <summary>Zrkadlo na seriovy port; null = ziadne.</summary>
    public string? Mirror { get; set; }

    /// <summary>Tabule na linke.</summary>
    public List<BoardRecord> Boards { get; set; } = [];
}

/// <summary>Tabula simulatora v databaze (tabulka <c>Boards</c>, kluc linka + adresa).</summary>
public sealed class BoardRecord
{
    public int Line { get; set; }

    public int Address { get; set; }

    public string Manufacturer { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Profil tabule (rozmery, pisma, hlavicka…) - vnoreny dokument, v databaze ako JSON.</summary>
    public BoardProfile Profile { get; set; } = new();

    /// <summary>Vzhlad; null = predvoleny.</summary>
    public string? Design { get; set; }

    /// <summary>Simulovana porucha - v databaze kluc (<c>none</c>, <c>noReply</c>…).</summary>
    public FaultKind FaultKind { get; set; }

    public int FaultDelayMs { get; set; }

    public int FaultStatus { get; set; }

    /// <summary>Druha adresa panelu; null = len jedna.</summary>
    public int? SecondAddress { get; set; }
}
