using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Boards.Announcements;

namespace TableSimulator.Core.Boards.History;

/// <summary>
/// Zbiera zmeny stavu svetla ustredni AK Signal a skoncene hlasenia zariadeni s hlaseniami (<see cref="IAnnouncementDisplay" />)
/// pre historiu - vola sa pod zamkom simulatora po kazdej zmene tabule, server ich odobera (<see cref="Take" />) a
/// zapisuje do databazy.
/// </summary>
internal sealed class DeviceHistoryCollector
{
    /// <summary>Kolko neodobratych zmien sa drzi (server odobera kazdych par sekund).</summary>
    private const int Capacity = 10_000;

    private readonly Dictionary<string, LightFlags> _last = [];
    private readonly Dictionary<string, long> _lastAnnouncement = [];
    private readonly List<LightSample> _lights = [];
    private readonly List<AnnouncementSample> _announcements = [];

    /// <summary>Stav svetla ustredne v bitoch.</summary>
    public static LightFlags FlagsOf(SimBoard board, AkSignalDisplay display) =>
        (display.AutoMode ? LightFlags.Auto : 0)
        | (display.Manual ? LightFlags.Manual : 0)
        | (display.Dark ? LightFlags.Dark : 0)
        | (display.IsLightOn ? LightFlags.On : 0)
        | ((board.EffectiveProfile.AkSignal ?? AkSignalProfile.Default).Conditions.LightsFault ? LightFlags.Fault : 0)
        | (display.ServerSilent ? LightFlags.Silent : 0)
        | (board.PoweredOff ? LightFlags.PoweredOff : 0);

    /// <summary>Zapamata si zmenu stavu svetla a nove skoncene hlasenia zariadenia; ina tabula sa preskoci.</summary>
    public void Observe(SimBoard board, DateTimeOffset now)
    {
        if (board.Display is not IAnnouncementDisplay device)
            return;

        // snimac este nevyhodnoteny (ustredna pred prvym tikom) - stav nie je co zapisat
        if (board.Display is AkSignalDisplay display && (display.DarkSince is not null || board.PoweredOff))
        {
            var flags = FlagsOf(board, display);
            if (!_last.TryGetValue(board.Id, out var last) || last != flags)
            {
                _last[board.Id] = flags;
                Add(_lights, new LightSample(board.Line, board.Address, now, flags));
            }
        }

        // obsah ustredne mohol vzniknut znova (iny vyrobca, nova tabula) - cisla hlaseni potom zacinaju odznova
        var seen = _lastAnnouncement.GetValueOrDefault(board.Id);
        var ended = device.Announcements.Where(a => a.EndedAt is not null).ToList();
        if (ended.Count > 0 && ended.Max(a => a.Id) < seen)
            seen = 0;
        foreach (var announcement in ended.Where(a => a.Id > seen))
        {
            Add(_announcements, new AnnouncementSample(board.Line, board.Address, announcement));
            seen = Math.Max(seen, announcement.Id);
        }

        _lastAnnouncement[board.Id] = seen;
    }

    /// <summary>Tabula zmizla - pri novej s rovnakou adresou sa stav zapise znova.</summary>
    public void Forget(string id)
    {
        _last.Remove(id);
        _lastAnnouncement.Remove(id);
    }

    /// <summary>Odoberie zmeny od posledneho odberu.</summary>
    public DeviceHistorySample Take()
    {
        var sample = new DeviceHistorySample([.. _lights], [.. _announcements]);
        _lights.Clear();
        _announcements.Clear();
        return sample;
    }

    private static void Add<T>(List<T> list, T item)
    {
        list.Add(item);
        if (list.Count > Capacity)
            list.RemoveAt(0);
    }
}
