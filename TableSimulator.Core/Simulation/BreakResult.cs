namespace TableSimulator.Core.Simulation;

/// <summary>Vysledok preruseni hlasenia (<see cref="Simulator.BreakAnnouncement" />).</summary>
public enum BreakResult
{
    /// <summary>Hlasenie sa prerusilo.</summary>
    Broken,

    /// <summary>Tabula nie je.</summary>
    NotFound,

    /// <summary>Tabula nie je rozhlas vzdialeneho INISSu.</summary>
    NotRemoteAudio,

    /// <summary>Nic nehra.</summary>
    NothingPlaying
}
