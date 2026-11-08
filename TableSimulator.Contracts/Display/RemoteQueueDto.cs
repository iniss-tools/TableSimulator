namespace TableSimulator.Contracts.Display;

/// <summary>Fronta hlaseni vzdialeneho INISSu.</summary>
/// <param name="Name">meno fronty (<c>QNAM</c>); "" = jedina fronta na linke</param>
/// <param name="Off">riadiaci INISS frontu vypol</param>
/// <param name="Playing">prave hrajuce hlasenie; null = nic</param>
public sealed record RemoteQueueDto(string Name, bool Off, AnnouncementDto? Playing)
{
    /// <summary>Posledna sprava riadiaceho INISSu pre frontu; null = ziadna.</summary>
    public DateTimeOffset? LastCommandAt { get; init; }
}
