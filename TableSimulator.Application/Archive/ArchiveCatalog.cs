using TableSimulator.Application.Settings;
using TableSimulator.Core.Archive;

namespace TableSimulator.Application.Archive;

/// <summary>
/// Denne archivy v sledovanych priecinkoch logov INISSu. Precitany subor si drzi (niekolko poslednych), kym sa
/// nezmeni - listovanie a filtrovanie ho nema citat znova.
/// </summary>
public sealed class ArchiveCatalog(ISettingsStore settings)
{
    private const int CacheSize = 4;

    private readonly Lock _lock = new();
    private readonly LinkedList<(ArchiveDay Day, ArchiveContent Content)> _cache = new();

    /// <summary>Dni archivu druhu <paramref name="kind" /> od najnovsieho.</summary>
    public IReadOnlyList<ArchiveDay> Days(ArchiveKind kind) => ArchiveFiles.Days(settings.Current.VirtualFolders, kind);

    /// <summary>Obsah archivu dna; null, ak taky den nie je.</summary>
    /// <exception cref="IOException">subor sa neda precitat</exception>
    public ArchiveContent? Read(ArchiveKind kind, DateOnly date)
    {
        if (Days(kind).FirstOrDefault(d => d.Date == date) is not { } day)
            return null;
        lock (_lock)
        {
            for (var node = _cache.First; node is not null; node = node.Next)
            {
                if (node.Value.Day != day)
                    continue;
                _cache.Remove(node);
                _cache.AddFirst(node);
                return node.Value.Content;
            }
        }

        // INISS do suboru prave pise - citat sa da aj tak
        using var stream = new FileStream(day.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var content = kind == ArchiveKind.Tables ? ArchiveReader.ReadTables(stream) : ArchiveReader.ReadReports(stream);
        lock (_lock)
        {
            _cache.AddFirst((day, content));
            while (_cache.Count > CacheSize)
                _cache.RemoveLast();
        }

        return content;
    }
}
