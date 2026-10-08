using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Station;

/// <summary>
/// Zalozenie liniek a tabul stanice INISSu v simulatore - spolocne pre import z GVDEditora (POST /api/station)
/// a nacitanie instalacie INISSu z nastavenia.
/// </summary>
public sealed class StationImporter(
    Simulator simulator,
    TransportManager transport,
    ISettingsStore settings,
    IVirtualBoardRepository virtualBoards,
    ILiveNotifier live)
{
    /// <summary>Zalozi linky, tabule a virtualne tabule stanice; priecinok logov prida k sledovanym.</summary>
    public async Task<StationImportedDto> ImportAsync(StationImportRequest request, CancellationToken cancellationToken)
    {
        var result = simulator.ImportStation(RequestMapper.ToImport(request));
        await transport.SyncAsync().ConfigureAwait(false);

        var folder = WatchLogFolder(request.LogFolder);
        var (added, kept) = await AddVirtualBoards(request.XmlTables ?? [], cancellationToken).ConfigureAwait(false);
        if (folder == StationLogFolder.Added)
            live.Publish(new StructureMessage());
        if (added > 0)
            live.Publish(new VirtualMessage([], true));
        return DtoMapper.ToDto(result) with { VirtualBoardsAdded = added, VirtualBoardsKept = kept, LogFolder = folder };
    }

    /// <summary>Priecinok logov INISSu prida k sledovanym, ak ho simulator vidi a este ho nesleduje.</summary>
    private StationLogFolder WatchLogFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return StationLogFolder.None;
        string path;
        try
        {
            path = Normalize(folder);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return StationLogFolder.Missing;
        }

        var current = settings.Current;
        if (current.VirtualFolders.Any(f => string.Equals(Normalize(f), path, StringComparison.OrdinalIgnoreCase)))
            return StationLogFolder.Watched;
        if (!Directory.Exists(path))
            return StationLogFolder.Missing;
        if (current.VirtualFolders.Count >= SettingsErrors.MaxVirtualFolders)
            return StationLogFolder.Full;
        settings.Save(current with { VirtualFolders = [.. current.VirtualFolders, path] });
        return StationLogFolder.Added;
    }

    private static string Normalize(string folder) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder.Trim()));

    /// <summary>Virtualne tabule pre XML, ktore este ziadna tabula neukazuje (cisla od najmensieho volneho).</summary>
    private async Task<(int Added, int Kept)> AddVirtualBoards(IReadOnlyList<StationXmlTableDto> tables, CancellationToken cancellationToken)
    {
        if (tables.Count == 0)
            return (0, 0);
        var existing = await virtualBoards.ListAsync(cancellationToken).ConfigureAwait(false);
        var sources = existing.Select(b => b.Source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ids = existing.Select(b => b.Id).ToHashSet();
        var boards = new List<VirtualBoard>();
        var kept = 0;
        foreach (var table in tables.DistinctBy(t => t.Source.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var source = table.Source.Trim();
            if (!sources.Add(source))
            {
                kept++;
                continue;
            }

            var id = 1;
            while (!ids.Add(id))
                id++;
            boards.Add(RequestMapper.ToVirtualBoard(id, table));
        }

        if (boards.Count > 0)
            await virtualBoards.ImportAsync(boards, false, cancellationToken).ConfigureAwait(false);
        return (boards.Count, kept);
    }
}
