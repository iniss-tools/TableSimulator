using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Simulation;

/// <summary>
/// Okolie simulatora, ktore zariadenia potrebuju a INISS ho neposiela: poloha pre snimac tmy a dlzky zvukov banky.
/// </summary>
public sealed record SimEnvironment
{
    /// <summary>Poloha pre vychod a zapad slnka.</summary>
    public GeoLocation Location { get; init; } = GeoLocation.Default;

    /// <summary>
    /// Dlzka zvuku banky podla mena, ako ho posiela INISS2 ustredni (<c>CZ/vlak/1234</c>) alebo INISS 3.39 vzdialenemu
    /// INISSu (<c>SK/R1/9900100</c>); null = dlzky nie su zname, zariadenie pouzije predvolenu dlzku
    /// (<see cref="AkSignalProfile.SoundSeconds" />, <see cref="RemoteAudioDisplay.DefaultSoundLength" />).
    /// </summary>
    public Func<string, TimeSpan?>? SoundLength { get; init; }
}

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
