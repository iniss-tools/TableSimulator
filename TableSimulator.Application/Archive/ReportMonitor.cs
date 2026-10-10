using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Contracts.Archive;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Archive;

namespace TableSimulator.Application.Archive;

/// <summary>
/// Monitor hlaseni: sleduje najnovsi denny archiv hlaseni v priecinkoch logov INISSu (<see cref="ReportTail" />)
/// a nove zaznamy posiela prehliadacom. Po polnoci prejde na novy subor.
/// </summary>
public sealed class ReportMonitor(ILiveNotifier live)
{
    /// <summary>Najviac zaznamov v prvom nacitani monitora.</summary>
    public const int MaxEntries = 1000;

    private readonly Lock _lock = new();
    private readonly ReportTail _tail = new();

    /// <summary>
    /// Pozrie sa na najnovsi archiv hlaseni v <paramref name="folders" /> a nove zaznamy posle prehliadacom.
    /// </summary>
    /// <exception cref="IOException">subor sa neda precitat</exception>
    public void Poll(IReadOnlyList<string> folders)
    {
        var days = ArchiveFiles.Days(folders, ArchiveKind.Reports);
        var day = days.Count > 0 ? days[0] : null;
        ReportsMessage message;
        lock (_lock)
        {
            var update = _tail.Update(day);
            if (update.IsEmpty)
                return;
            message = new ReportsMessage(_tail.Day?.Date, update.Reset ? [] : update.Added.Select(DtoMapper.ToDto).ToArray(), update.Reset,
                _tail.Entries.Count, _tail.Skipped);
        }

        live.Publish(message);
    }

    /// <summary>Najnovsich <paramref name="limit" /> zaznamov od najnovsieho.</summary>
    public ReportMonitorDto Snapshot(int limit)
    {
        lock (_lock)
        {
            var entries = _tail.Entries;
            var newest = entries.Skip(Math.Max(0, entries.Count - limit)).Reverse().Select(DtoMapper.ToDto).ToArray();
            return new ReportMonitorDto(_tail.Day?.Date, _tail.Day is { } day ? Path.GetFileName(day.Path) : null, newest, entries.Count, _tail.Skipped);
        }
    }
}
