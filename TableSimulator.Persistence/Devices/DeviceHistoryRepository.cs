using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Devices;
using TableSimulator.Core.Boards.History;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Persistence.Devices;

/// <summary>Historia ustredni AK Signal v databaze - zmeny stavu svetla a skoncene hlasenia.</summary>
public sealed class DeviceHistoryRepository(AppDbContext db) : IDeviceHistoryRepository
{
    public async Task AddAsync(DeviceHistorySample sample, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sample);
        foreach (var light in sample.Lights)
            db.DeviceLightStates.Add(new LightStateRecord(0, light.Line, light.Address, light.At.ToUnixTimeMilliseconds(), (int)light.Flags));

        foreach (var item in sample.Announcements)
        {
            var a = item.Announcement;
            var sounds = string.Join("*", a.Sounds);
            db.DeviceAnnouncements.Add(new AnnouncementRecord(0, item.Line, item.Address, a.Channel, a.StartedAt.ToUnixTimeMilliseconds(),
                (a.EndedAt ?? a.StartedAt).ToUnixTimeMilliseconds(), SimulatorConfigJson.Key(a.Result),
                sounds.Length > DeviceHistoryRetention.MaxSounds ? sounds[..DeviceHistoryRetention.MaxSounds] : sounds, a.Value,
                Cut(a.Title, DeviceHistoryRetention.MaxTitle), Cut(a.Queue, DeviceHistoryRetention.MaxName), Cut(a.Circuit, DeviceHistoryRetention.MaxName)));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<DeviceHistoryData> QueryAsync(long fromMs, long toMs, int? line, int? address, CancellationToken cancellationToken)
    {
        var lights = db.DeviceLightStates.AsNoTracking().Where(s => s.TimeMs >= fromMs && s.TimeMs < toMs);
        // stav na zaciatku obdobia = posledna zmena pred nim
        var before = db.DeviceLightStates.AsNoTracking()
            .Where(s => s.TimeMs < fromMs
                        && s.TimeMs == db.DeviceLightStates.Where(x => x.Line == s.Line && x.Address == s.Address && x.TimeMs < fromMs).Max(x => x.TimeMs));
        var announcements = db.DeviceAnnouncements.AsNoTracking().Where(a => a.StartMs < toMs && a.EndMs >= fromMs);
        if (line is { } l)
        {
            lights = lights.Where(s => s.Line == l);
            before = before.Where(s => s.Line == l);
            announcements = announcements.Where(a => a.Line == l);
        }

        if (address is { } a)
        {
            lights = lights.Where(s => s.Address == a);
            before = before.Where(s => s.Address == a);
            announcements = announcements.Where(x => x.Address == a);
        }

        var states = await before.ToListAsync(cancellationToken).ConfigureAwait(false);
        states.AddRange(await lights.ToListAsync(cancellationToken).ConfigureAwait(false));
        return new DeviceHistoryData(states, await announcements.ToListAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task<int> DeleteOlderThanAsync(long timeMs, CancellationToken cancellationToken)
    {
        // posledny stav kazdej ustredne ostava - je to stav na zaciatku uchovanej historie
        var deleted = await db.DeviceLightStates
            .Where(s => s.TimeMs < timeMs
                        && s.TimeMs < db.DeviceLightStates.Where(x => x.Line == s.Line && x.Address == s.Address && x.TimeMs < timeMs).Max(x => x.TimeMs))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        deleted += await db.DeviceAnnouncements.Where(a => a.EndMs < timeMs).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        return deleted;
    }

    private static string? Cut(string? text, int max) => text is { Length: var length } && length > max ? text[..max] : text;
}
