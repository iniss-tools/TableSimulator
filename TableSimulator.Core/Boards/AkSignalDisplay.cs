namespace TableSimulator.Core.Boards;

/// <summary>
/// Obsah, ktory sa meni aj bez spravy INISSu - podla casu (snimac tmy, koniec hlasenia).
/// </summary>
public interface ITimedDisplay
{
    /// <summary>Posunie obsah do casu <paramref name="now" />; vrati true, ak sa zmenil.</summary>
    /// <param name="now">aktualny cas</param>
    /// <param name="profile">profil tabule</param>
    /// <param name="location">poloha pre vychod a zapad slnka</param>
    bool Advance(DateTimeOffset now, BoardProfile profile, GeoLocation location);
}

/// <summary>
/// Ustredna AK Signal: svetlo (automatika a rucne svietenie zapina server, tmu hlasi snimac) a rozhlas - pri AKSIGNAL
/// len spinanie (zvuk hra server), pri AKSIGNALUH dva kanaly, ktore hraju zvuky samy. Stavy ustredne (poruchy,
/// vyssia priorita) su v profile (<see cref="AkSignalProfile.Conditions" />). Viz docs/iniss2/protokoly/aksignal.mdx.
/// </summary>
public sealed class AkSignalDisplay : BoardDisplay, ITimedDisplay, IAnnouncementDisplay
{
    /// <summary>Kolko poslednych hlaseni ustredna drzi.</summary>
    public const int MaxAnnouncements = 50;

    /// <summary>Bit stavu zvuku AKSIGNAL: rozhlas zapnuty.</summary>
    public const byte AudioPlaying = 0x01;

    /// <summary>Bit stavu zvuku AKSIGNAL: bezi hlasenie s vyssou prioritou.</summary>
    public const byte AudioHighPriority = 0x02;

    /// <summary>Bit stavu zvuku AKSIGNAL: hlasenie zrusilo hlasenie s vyssou prioritou.</summary>
    public const byte AudioCancelled = 0x04;

    /// <summary>Bit stavu zvuku AKSIGNAL: chyba vystupu.</summary>
    public const byte AudioOutputError = 0x08;

    /// <summary>Bit stavu zvuku AKSIGNAL: napajanie zosilnovaca v poriadku.</summary>
    public const byte AudioAmpPower = 0x10;

    /// <summary>Bit stavu svetla: automaticke svietenie.</summary>
    public const byte LightAuto = 0x01;

    /// <summary>Bit stavu svetla: rucne svietenie.</summary>
    public const byte LightManual = 0x02;

    /// <summary>Bit stavu svetla: je tma.</summary>
    public const byte LightDark = 0x04;

    /// <summary>Bit stavu svetla: svetla v poriadku.</summary>
    public const byte LightOk = 0x08;

    // kanal 0 = rozhlas AKSIGNAL, 1 a 2 = kanaly AKSIGNALUH
    private readonly Announcement?[] _playing = new Announcement?[3];
    private readonly bool[] _cancelled = new bool[3];
    private readonly List<Announcement> _history = [];
    private Dictionary<string, string> _banks = new(StringComparer.Ordinal);
    private long _nextId;

    /// <summary>Automaticke svietenie - zapina a vypina ho server podla vlakov.</summary>
    public bool AutoMode { get; private set; }

    /// <summary>Rucne svietenie - zapina ho obsluha servera.</summary>
    public bool Manual { get; private set; }

    /// <summary>Snimac hlasi tmu.</summary>
    public bool Dark { get; private set; }

    /// <summary>Od kedy snimac hlasi terajsi stav; null = este nevyhodnoteny.</summary>
    public DateTimeOffset? DarkSince { get; private set; }

    /// <summary>Kedy snimac podla slnka zmeni stav; null = snimac nastaveny natrvalo alebo polarny den ci noc.</summary>
    public DateTimeOffset? DarkChangesAt { get; private set; }

    /// <summary>Svetla svietia: rucne, alebo automatika a tma.</summary>
    public bool IsLightOn => Manual || AutoMode && Dark;

    /// <summary>Kedy sa svetlo naposledy rozsvietilo alebo zhaslo; null = od zapnutia nie.</summary>
    public DateTimeOffset? LightChangedAt { get; private set; }

    /// <summary>Posledny prikaz servera pre svetlo; null = ziadny.</summary>
    public DateTimeOffset? LightQueryAt { get; private set; }

    /// <summary>Posledny prikaz servera pre rozhlas; null = ziadny.</summary>
    public DateTimeOffset? AudioQueryAt { get; private set; }

    /// <summary>Server sa ustredne dlhsie nepyta (<see cref="AkSignalProfile.SilenceSeconds" />).</summary>
    public bool ServerSilent { get; private set; }

    /// <summary>Banky zvukov z posledneho prikazu <c>sync</c>: priecinok jazyka → MD5 jeho FyzBank.xml.</summary>
    public IReadOnlyDictionary<string, string> Banks => _banks;

    /// <summary>Posledny prikaz <c>sync</c>; null = ziadny.</summary>
    public DateTimeOffset? SyncedAt { get; private set; }

    /// <summary>Do kedy ustredna po <c>sync</c> hlasi nezosuladenu banku; null = nehlasi.</summary>
    public DateTimeOffset? SyncUntil { get; private set; }

    /// <summary>Posledne hlasenia od najstarsieho vratane prave hrajucich.</summary>
    public IReadOnlyList<Announcement> Announcements =>
        [.. _history, .. _playing.OfType<Announcement>().OrderBy(a => a.StartedAt).ThenBy(a => a.Id)];

    /// <summary>Hlasenie, ktore prave hra na kanali (0 = rozhlas AKSIGNAL); null = nic.</summary>
    public Announcement? Playing(int channel) => channel is >= 0 and <= 2 ? _playing[channel] : null;

    /// <summary>Kanal hlasi, ze hlasenie zrusilo hlasenie s vyssou prioritou (kym ho server nezastavi).</summary>
    public bool IsCancelled(int channel) => channel is >= 0 and <= 2 && _cancelled[channel];

    /// <summary>Posledny dotaz servera na ustrednu; null = ziadny.</summary>
    public DateTimeOffset? LastQueryAt => (LightQueryAt, AudioQueryAt) switch
    {
        ({ } light, { } audio) => light > audio ? light : audio,
        ({ } light, null) => light,
        (null, var audio) => audio
    };

    /// <inheritdoc />
    public bool Advance(DateTimeOffset now, BoardProfile profile, GeoLocation location)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var settings = profile.AkSignal ?? AkSignalProfile.Default;
        var changed = false;

        var dark = settings.Sensor.Mode switch
        {
            TwilightMode.Dark => true,
            TwilightMode.Light => false,
            _ => SunTimes.IsDark(now, location, settings.Sensor.OffsetMinutes)
        };

        // cas zmeny len pri zmene alebo po nej - pocitat ho kazdu sekundu netreba
        var sun = settings.Sensor.Mode == TwilightMode.Sun;
        if (!sun)
            DarkChangesAt = null;
        else if (DarkChangesAt is null || DarkChangesAt <= now || dark != Dark)
            DarkChangesAt = SunTimes.NextChange(now, location, settings.Sensor.OffsetMinutes);
        if (dark != Dark || DarkSince is null)
        {
            var wasOn = IsLightOn;
            Dark = dark;
            DarkSince = now;
            if (IsLightOn != wasOn)
                LightChangedAt = now;
            changed = true;
        }

        // hlasenie s vyssou prioritou zrusi hlasenie servera, ustredna to hlasi, kym ju server nezastavi
        for (var channel = 0; channel < _playing.Length; channel++)
        {
            if (_playing[channel] is not { } playing)
                continue;
            if (settings.Conditions.HighPriority)
            {
                Finish(channel, now, AnnouncementResult.Cancelled);
                _cancelled[channel] = true;
                changed = true;
            }
            else if (playing.PlannedEnd is { } end && end <= now)
            {
                Finish(channel, end, AnnouncementResult.Completed);
                changed = true;
            }
        }

        if (SyncUntil is { } until && until <= now)
        {
            SyncUntil = null;
            changed = true;
        }

        var silent = LastQueryAt is { } last && now - last > TimeSpan.FromSeconds(Math.Max(1, settings.SilenceSeconds));
        if (silent != ServerSilent)
        {
            ServerSilent = silent;
            changed = true;
        }

        return changed;
    }

    /// <summary>Dotaz servera na svetlo; vrati true, ak sa server znova ozval.</summary>
    public bool QueryLight(DateTimeOffset now)
    {
        LightQueryAt = now;
        return Heard();
    }

    /// <summary>Dotaz servera na rozhlas; vrati true, ak sa server znova ozval.</summary>
    public bool QueryAudio(DateTimeOffset now)
    {
        AudioQueryAt = now;
        return Heard();
    }

    /// <summary>Zapne alebo vypne automaticke svietenie; vrati true, ak sa zmenilo.</summary>
    public bool SetAuto(bool on, DateTimeOffset now) => SetLight(on, Manual, now);

    /// <summary>Zapne alebo zrusi rucne svietenie; vrati true, ak sa zmenilo.</summary>
    public bool SetManual(bool on, DateTimeOffset now) => SetLight(AutoMode, on, now);

    /// <summary>Stav svetla AKSIGNAL (bity <see cref="LightAuto" />…).</summary>
    public byte LightState(BoardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var conditions = (profile.AkSignal ?? AkSignalProfile.Default).Conditions;
        return (byte)((AutoMode ? LightAuto : 0) | (Manual ? LightManual : 0) | (Dark ? LightDark : 0) | (conditions.LightsFault ? 0 : LightOk));
    }

    /// <summary>Stav svetla AKSIGNALUH: kluce <c>lit</c>, <c>man</c>, <c>dark</c>, <c>ok</c>, <c>on</c>.</summary>
    public IReadOnlyList<string> LightKeys(BoardProfile profile)
    {
        var state = LightState(profile);
        var keys = new List<string>();
        if ((state & LightAuto) != 0)
            keys.Add("lit");
        if ((state & LightManual) != 0)
            keys.Add("man");
        if ((state & LightDark) != 0)
            keys.Add("dark");
        if ((state & LightOk) != 0)
            keys.Add("ok");
        if (IsLightOn)
            keys.Add("on");
        return keys;
    }

    /// <summary>
    /// Zapne rozhlas AKSIGNAL s hodnotou <paramref name="value" />; pri hlaseni s vyssou prioritou ho ustredna hned zrusi.
    /// Vrati true, ak sa stav zmenil.
    /// </summary>
    public bool AudioOn(int value, DateTimeOffset now, BoardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _cancelled[0] = false;
        if (_playing[0] is { } playing)
        {
            if (playing.Value == value)
                return false;
            _playing[0] = playing with { Value = value };
            return true;
        }

        _playing[0] = new Announcement(++_nextId, 0, [], now) { Value = value };
        if ((profile.AkSignal ?? AkSignalProfile.Default).Conditions.HighPriority)
        {
            Finish(0, now, AnnouncementResult.Cancelled);
            _cancelled[0] = true;
        }

        return true;
    }

    /// <summary>Vypne rozhlas AKSIGNAL; vrati true, ak bol zapnuty alebo hlasil zrusenie.</summary>
    public bool AudioOff(DateTimeOffset now)
    {
        var changed = _cancelled[0] || _playing[0] is not null;
        _cancelled[0] = false;
        if (_playing[0] is not null)
            Finish(0, now, AnnouncementResult.Completed);
        return changed;
    }

    /// <summary>Stav zvuku AKSIGNAL (bity <see cref="AudioPlaying" />…).</summary>
    public byte AudioState(BoardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var conditions = (profile.AkSignal ?? AkSignalProfile.Default).Conditions;
        return (byte)((_playing[0] is not null ? AudioPlaying : 0)
                      | (conditions.HighPriority ? AudioHighPriority : 0)
                      | (_cancelled[0] ? AudioCancelled : 0)
                      | (conditions.OutputPowerError ? AudioOutputError : 0)
                      | (conditions.AmpSupplyMissing ? 0 : AudioAmpPower));
    }

    /// <summary>
    /// Spusti hlasenie AKSIGNALUH na kanali 1 alebo 2 na dobu <paramref name="duration" />. Pri hlaseni s vyssou
    /// prioritou alebo bez napajania zosilnovaca ustredna nehra. Prave hrajuce hlasenie kanala zastavi. Vrati true, ak
    /// zacalo hrat.
    /// </summary>
    public bool Play(int channel, IReadOnlyList<string> sounds, TimeSpan duration, DateTimeOffset now, BoardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(sounds);
        ArgumentNullException.ThrowIfNull(profile);
        if (channel is not (1 or 2))
            return false;

        _cancelled[channel] = false;
        var conditions = (profile.AkSignal ?? AkSignalProfile.Default).Conditions;
        if (conditions.HighPriority || conditions.AmpSupplyMissing)
            return false;

        if (_playing[channel] is not null)
            Finish(channel, now, AnnouncementResult.Stopped);
        _playing[channel] = new Announcement(++_nextId, channel, sounds, now) { Duration = duration };
        return true;
    }

    /// <summary>Zastavi kanal AKSIGNALUH; vrati true, ak hral alebo hlasil zrusenie.</summary>
    public bool Stop(int channel, DateTimeOffset now)
    {
        if (channel is not (1 or 2))
            return false;

        var changed = _cancelled[channel] || _playing[channel] is not null;
        _cancelled[channel] = false;
        if (_playing[channel] is not null)
            Finish(channel, now, AnnouncementResult.Stopped);
        return changed;
    }

    /// <summary>Prikaz <c>sync</c>: zapamata si banky a <see cref="AkSignalProfile.SyncSeconds" /> hlasi, ze ich zosuladuje.</summary>
    public void Sync(IReadOnlyDictionary<string, string> banks, DateTimeOffset now, BoardProfile profile)
    {
        ArgumentNullException.ThrowIfNull(banks);
        ArgumentNullException.ThrowIfNull(profile);
        _banks = new Dictionary<string, string>(banks, StringComparer.Ordinal);
        SyncedAt = now;
        var seconds = (profile.AkSignal ?? AkSignalProfile.Default).SyncSeconds;
        SyncUntil = seconds > 0 ? now.AddSeconds(seconds) : null;
    }

    /// <summary>
    /// Stav zvuku AKSIGNALUH: kluce <c>run</c>, <c>hiprt</c>, <c>cancl</c>, <c>pwr</c>, <c>sup</c>, <c>comerr</c>,
    /// <c>sync</c>. Kanal null (dotaz <c>&lt;aud&gt;</c>) hlasi hranie a zrusenie ktorehokolvek kanala.
    /// </summary>
    public IReadOnlyList<string> AudioKeys(BoardProfile profile, int? channel, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var conditions = (profile.AkSignal ?? AkSignalProfile.Default).Conditions;
        int[] channels = channel is (1 or 2) and var one ? [one] : [1, 2];

        var keys = new List<string>();
        if (channels.Any(c => _playing[c] is not null))
            keys.Add("run");
        if (conditions.HighPriority)
            keys.Add("hiprt");
        if (channels.Any(c => _cancelled[c]))
            keys.Add("cancl");
        if (conditions.OutputPowerError)
            keys.Add("pwr");
        if (conditions.AmpSupplyMissing)
            keys.Add("sup");
        if (conditions.AmpCommError)
            keys.Add("comerr");
        if (conditions.BankNotSynced || SyncUntil > now)
            keys.Add("sync");
        return keys;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        Array.Clear(_playing);
        Array.Clear(_cancelled);
        _history.Clear();
        _banks = new Dictionary<string, string>(StringComparer.Ordinal);
        AutoMode = false;
        Manual = false;
        Dark = false;
        DarkSince = null;
        DarkChangesAt = null;
        LightChangedAt = null;
        LightQueryAt = null;
        AudioQueryAt = null;
        ServerSilent = false;
        SyncedAt = null;
        SyncUntil = null;
    }

    private bool SetLight(bool auto, bool manual, DateTimeOffset now)
    {
        if (auto == AutoMode && manual == Manual)
            return false;
        var wasOn = IsLightOn;
        AutoMode = auto;
        Manual = manual;
        if (IsLightOn != wasOn)
            LightChangedAt = now;
        return true;
    }

    private bool Heard()
    {
        if (!ServerSilent)
            return false;
        ServerSilent = false;
        return true;
    }

    private void Finish(int channel, DateTimeOffset at, AnnouncementResult result)
    {
        if (_playing[channel] is not { } playing)
            return;
        _playing[channel] = null;
        _history.Add(playing.End(at, result));
        if (_history.Count > MaxAnnouncements)
            _history.RemoveAt(0);
    }
}
