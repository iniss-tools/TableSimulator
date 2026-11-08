namespace TableSimulator.Contracts.Display;

/// <summary>Rozhlas vzdialeneho INISSu (rodina <c>remoteaudio</c>): fronty hlaseni a posledne hlasenia.</summary>
/// <param name="Queues">fronty v poradi, v akom ich riadiaci INISS prvy raz poslal</param>
/// <param name="Announcements">posledne hlasenia od najstarsieho vratane prave hrajucich</param>
public sealed record RemoteAudioDto(IReadOnlyList<RemoteQueueDto> Queues, IReadOnlyList<AnnouncementDto> Announcements)
{
    /// <summary>Posledna sprava riadiaceho INISSu s frontami; null = ziadna.</summary>
    public DateTimeOffset? LastQueryAt { get; init; }
}
