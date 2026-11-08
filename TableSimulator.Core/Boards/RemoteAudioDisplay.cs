using System.Globalization;

namespace TableSimulator.Core.Boards;

/// <summary>
/// Nahravka zo zoznamu <c>RWAV</c> vzdialeneho INISSu: odkaz do banky <c>jazyk/skupina/zvuk</c> a pauzy pred a po
/// nahravke [ms]. Viz docs/iniss/protokoly/vzdialeny-iniss.mdx.
/// </summary>
/// <param name="Name">odkaz do banky (kluc jazyka, skupiny a zvuku)</param>
/// <param name="PauseBefore">ticho pred nahravkou [ms]</param>
/// <param name="PauseAfter">ticho po nahravke [ms]</param>
public sealed record RemoteSound(string Name, int PauseBefore, int PauseAfter)
{
    /// <summary>
    /// Zoznam <c>RWAV</c>: trojice <c>odkaz,pauza pred,pauza po</c> oddelene ciarkou. Cislo, ktore sa neda precitat, je
    /// 0 (ako vo vzdialenom INISSe); prazdny odkaz sa vynecha.
    /// </summary>
    public static IReadOnlyList<RemoteSound> Parse(string recordings)
    {
        ArgumentNullException.ThrowIfNull(recordings);
        var parts = recordings.Split(',');
        var sounds = new List<RemoteSound>();
        for (var i = 0; i < parts.Length; i += 3)
        {
            var name = parts[i].Trim();
            if (name.Length > 0)
                sounds.Add(new RemoteSound(name, Number(parts, i + 1), Number(parts, i + 2)));
        }

        return sounds;
    }

    /// <summary>
    /// Dlzka hlasenia: sucet nahravok s pauzami. Dlzku nahravky da <paramref name="length" />; nezname nahravky maju
    /// <paramref name="fallback" />.
    /// </summary>
    public static TimeSpan Duration(IEnumerable<RemoteSound> sounds, Func<string, TimeSpan?>? length, TimeSpan fallback)
    {
        ArgumentNullException.ThrowIfNull(sounds);
        var total = TimeSpan.Zero;
        foreach (var sound in sounds)
        {
            var one = TimeSpan.FromMilliseconds(sound.PauseBefore + sound.PauseAfter) + (length?.Invoke(sound.Name) ?? fallback);
            if (one > TimeSpan.Zero)
                total += one;
        }

        return total;
    }

    private static int Number(string[] parts, int index) =>
        index < parts.Length && int.TryParse(parts[index].Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : 0;
}

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

/// <summary>
/// Rozhlas vzdialeneho INISSu (linka triedy 128): fronty hlaseni, ktore riadiaci INISS posiela v <c>RWAV</c>.
/// Hlasenie trva podla dlzok nahravok v banke (<see cref="Simulation.SimEnvironment.SoundLength" />) a pauz zo
/// zoznamu; kym hra, stav fronty (<c>RSTA</c>) ma bit <c>0x100</c>. Obsluha simulatora ho moze prerusit - riadiaci INISS
/// potom zapise <c>remote break</c>.
/// </summary>
public sealed class RemoteAudioDisplay : BoardDisplay, ITimedDisplay, IAnnouncementDisplay
{
    /// <summary>Kolko poslednych hlaseni zariadenie drzi.</summary>
    public const int MaxAnnouncements = 50;

    /// <summary>Stav fronty: zapnuta, nic necaka.</summary>
    public const int StateIdle = 1;

    /// <summary>Stav fronty: vypnuta.</summary>
    public const int StateOff = 2;

    /// <summary>Stav fronty: zapnuta, hlasenia cakaju.</summary>
    public const int StateWaiting = 3;

    /// <summary>Bit stavu: hlasenie prevzate alebo hra.</summary>
    public const int StatePlaying = 0x100;

    /// <summary>Bit stavu: ozvena bitu <see cref="CommandKeepPlaying" /> z prikazu.</summary>
    public const int StateKeepPlayingEcho = 0x400;

    /// <summary>Prikaz fronty: pokracovat v prevzatom hlaseni.</summary>
    public const int CommandKeepPlaying = 0x1;

    /// <summary>Prikaz fronty: ziadost o zapnutie alebo vypnutie.</summary>
    public const int CommandSwitch = 0x10000000;

    /// <summary>Prikaz fronty: fronta ma byt vypnuta.</summary>
    public const int CommandSwitchOff = 0x20000000;

    /// <summary>Dlzka nahravky, ktoru banka nepozna.</summary>
    public static readonly TimeSpan DefaultSoundLength = TimeSpan.FromSeconds(1.5);

    private readonly List<RemoteQueue> _queues = [];
    private readonly List<Announcement> _history = [];
    private long _nextId;

    /// <summary>Fronty v poradi, v akom ich riadiaci INISS prvy raz poslal.</summary>
    public IReadOnlyList<RemoteQueue> Queues => _queues;

    /// <summary>Posledne hlasenia od najstarsieho vratane prave hrajucich.</summary>
    public IReadOnlyList<Announcement> Announcements =>
        [.. _history, .. _queues.Select(q => q.Playing).OfType<Announcement>().OrderBy(a => a.StartedAt).ThenBy(a => a.Id)];

    /// <summary>Posledna sprava riadiaceho INISSu s frontami; null = ziadna.</summary>
    public DateTimeOffset? LastQueryAt { get; private set; }

    /// <summary>Fronta podla mena ("" = jedina fronta); null = riadiaci INISS ju este neposlal.</summary>
    public RemoteQueue? Find(string name) => _queues.FirstOrDefault(q => q.Name == name);

    /// <summary>
    /// Spracuje frontu zo spravy riadiaceho INISSu a vrati jej stav pre <c>RSTA</c>; <paramref name="changed" /> = zmenilo
    /// sa hlasenie alebo zapnutie fronty.
    /// </summary>
    /// <param name="name">meno fronty ("" = jedina)</param>
    /// <param name="command">prikaz <c>RCMD</c></param>
    /// <param name="recordings">zoznam <c>RWAV</c>; null = sprava ho nema (hlasenie uz je prevzate alebo ziadne nie je)</param>
    /// <param name="title">titulok <c>RTIT</c></param>
    /// <param name="circuit">okruh <c>LINE</c></param>
    /// <param name="now">cas spravy</param>
    /// <param name="soundLength">dlzka nahravky z banky</param>
    /// <param name="changed">stav sa zmenil</param>
    public int Handle(string name, int command, string? recordings, string? title, string? circuit, DateTimeOffset now,
        Func<string, TimeSpan?>? soundLength, out bool changed)
    {
        ArgumentNullException.ThrowIfNull(name);
        changed = Advance(now);
        LastQueryAt = now;
        var queue = Find(name);
        if (queue is null)
        {
            queue = new RemoteQueue(name);
            _queues.Add(queue);
            changed = true;
        }

        queue.LastCommandAt = now;
        if ((command & CommandSwitch) != 0)
        {
            var off = (command & CommandSwitchOff) != 0;
            if (queue.Off != off)
            {
                queue.Off = off;
                changed = true;
                if (off)
                    Finish(queue, now, AnnouncementResult.Stopped);
            }
        }

        // prerusene hlasenie: odpoved bez stavu fronty - riadiaci INISS ho ukonci s vysledkom remote break
        if (queue.BreakPending)
        {
            queue.BreakPending = false;
            return 0;
        }

        if (recordings is not null && !queue.Off)
        {
            var request = (recordings, title, circuit);
            if (queue.Playing is null || queue.Request != request)
            {
                Finish(queue, now, AnnouncementResult.Stopped);
                var sounds = RemoteSound.Parse(recordings);
                queue.Playing = new Announcement(++_nextId, 0, sounds.Select(s => s.Name).ToList(), now)
                {
                    Title = string.IsNullOrEmpty(title) ? null : title,
                    Duration = RemoteSound.Duration(sounds, soundLength, DefaultSoundLength),
                    Queue = name,
                    Circuit = string.IsNullOrEmpty(circuit) ? null : circuit
                };
                queue.Request = request;
                queue.Taken = false;
                changed = true;
            }
        }
        else if (recordings is null && queue.Playing is not null)
        {
            // prevzate hlasenie bez bitu "pokracovat" - riadiaci INISS ho zastavil
            if ((command & CommandKeepPlaying) != 0)
                queue.Taken = true;
            else if (queue.Taken)
            {
                Finish(queue, now, AnnouncementResult.Stopped);
                changed = true;
            }
        }

        var state = queue.Off ? StateOff : queue.Playing is not null ? StatePlaying | StateWaiting : StateIdle;
        // poziadavku o zapnutie/vypnutie potvrdi ozvenou bitov, riadiaci INISS ju potom prestane posielat
        state |= command & (CommandSwitch | CommandSwitchOff);
        if ((command & CommandKeepPlaying) != 0)
            state |= StateKeepPlayingEcho;
        return state;
    }

    /// <summary>
    /// Prerusi prave hrajuce hlasenie fronty (null = vsetkych front): skonci ako zrusene a dalsia odpoved riadiacemu
    /// INISSu nebude mat stav fronty, takze ho zapise ako <c>remote break</c>. Vrati true, ak nieco hralo.
    /// </summary>
    public bool Break(string? name, DateTimeOffset now)
    {
        var broke = false;
        foreach (var queue in _queues.Where(q => (name is null || q.Name == name) && q.Playing is not null))
        {
            Finish(queue, now, AnnouncementResult.Cancelled);
            queue.BreakPending = true;
            broke = true;
        }

        return broke;
    }

    /// <inheritdoc />
    public bool Advance(DateTimeOffset now, BoardProfile profile, GeoLocation location) => Advance(now);

    /// <inheritdoc />
    public override void Clear()
    {
        _queues.Clear();
        _history.Clear();
        LastQueryAt = null;
    }

    /// <summary>Ukonci dohrane hlasenia; vrati true, ak nejake skoncilo.</summary>
    private bool Advance(DateTimeOffset now)
    {
        var changed = false;
        foreach (var queue in _queues)
        {
            if (queue.Playing is { PlannedEnd: { } end } && end <= now)
            {
                Finish(queue, end, AnnouncementResult.Completed);
                changed = true;
            }
        }

        return changed;
    }

    private void Finish(RemoteQueue queue, DateTimeOffset at, AnnouncementResult result)
    {
        if (queue.Playing is not { } playing)
            return;
        queue.Playing = null;
        queue.Request = null;
        queue.Taken = false;
        _history.Add(playing.End(at, result));
        if (_history.Count > MaxAnnouncements)
            _history.RemoveAt(0);
    }
}
