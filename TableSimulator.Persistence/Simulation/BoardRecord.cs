using TableSimulator.Core.Boards;

namespace TableSimulator.Persistence.Simulation;

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
