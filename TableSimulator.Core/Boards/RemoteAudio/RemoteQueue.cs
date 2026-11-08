using TableSimulator.Core.Boards.Announcements;

namespace TableSimulator.Core.Boards.RemoteAudio;

/// <summary>Fronta hlaseni vzdialeneho INISSu (<c>QNAM</c>; jedina fronta bez mena ma meno "").</summary>
public sealed class RemoteQueue(string name)
{
    /// <summary>Meno fronty; "" = jedina fronta na linke.</summary>
    public string Name { get; } = name;

    /// <summary>Riadiaci INISS frontu vypol.</summary>
    public bool Off { get; internal set; }

    /// <summary>Prave hrajuce hlasenie; null = nic.</summary>
    public Announcement? Playing { get; internal set; }

    /// <summary>Riadiaci INISS potvrdil prevzatie hlasenia (bit <c>0x1</c> v <c>RCMD</c>).</summary>
    public bool Taken { get; internal set; }

    /// <summary>Posledna sprava riadiaceho INISSu pre frontu; null = ziadna.</summary>
    public DateTimeOffset? LastCommandAt { get; internal set; }

    /// <summary>Hlasenie sa prerusilo - dalsia odpoved bude bez stavu fronty (<c>remote break</c>).</summary>
    internal bool BreakPending { get; set; }

    /// <summary>Zoznam, titulok a okruh hrajuceho hlasenia - ta ista ziadost ho nespusti znova.</summary>
    internal (string Recordings, string? Title, string? Circuit)? Request { get; set; }
}
