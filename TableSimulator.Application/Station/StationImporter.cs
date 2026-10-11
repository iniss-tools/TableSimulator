using TableSimulator.Application.Faces;
using TableSimulator.Application.Live;
using TableSimulator.Application.Mapping;
using TableSimulator.Application.Settings;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Station;

/// <summary>
/// Zalozenie liniek a tabul stanice INISSu v simulatore - spolocne pre import z GVDEditora (POST /api/station)
/// a nacitanie instalacie INISSu z nastavenia. Novym tabulam (na vyziadanie aj tabuliam bez cela) zalozi celo z dat
/// stanice (<see cref="StationFaces" />) - rovnake cela zdielaju jednu predlohu.
/// </summary>
public sealed class StationImporter(
    Simulator simulator,
    TransportManager transport,
    ISettingsStore settings,
    IVirtualBoardRepository virtualBoards,
    IFaceRepository faces,
    ILiveNotifier live)
{
    /// <summary>Kluc predlohy cela z dat stanice bez druhu tabule.</summary>
    private const string FacePrefix = "stanica";

    /// <summary>Zalozi linky, tabule a virtualne tabule stanice; priecinok logov prida k sledovanym.</summary>
    public async Task<StationImportedDto> ImportAsync(StationImportRequest request, CancellationToken cancellationToken)
    {
        var import = RequestMapper.ToImport(request);
        var (planned, created) = await PlanFacesAsync(request, cancellationToken).ConfigureAwait(false);
        import = import with
        {
            Boards = import.Boards.Select((b, i) => planned[i] is { } f ? b with { Face = f.Id, FaceValues = f.Values } : b).ToList()
        };
        var result = simulator.ImportStation(import);
        await transport.SyncAsync().ConfigureAwait(false);

        // ulozia sa len predlohy, ktore niektora tabula dostala (tabule mimo simulatora ich nepotrebuju)
        var used = result.FacesAssigned.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var facesAdded = 0;
        foreach (var face in created.Where(f => used.Contains(f.Id)))
        {
            await faces.SaveAsync(new FaceRecord(face.Id, face.Name, BoardFaces.Serialize(face)), cancellationToken).ConfigureAwait(false);
            facesAdded++;
        }

        if (facesAdded > 0)
            live.Publish(new FacesMessage());

        var folder = WatchLogFolder(request.LogFolder);
        var (added, kept) = await AddVirtualBoards(request.XmlTables ?? [], cancellationToken).ConfigureAwait(false);
        if (folder == StationLogFolder.Added)
            live.Publish(new StructureMessage());
        if (added > 0)
            live.Publish(new VirtualMessage([], true));
        return DtoMapper.ToDto(result) with { VirtualBoardsAdded = added, VirtualBoardsKept = kept, LogFolder = folder, FacesAdded = facesAdded };
    }

    /// <summary>
    /// Cela z dat stanice pre tabule importu (v poradi tabul, null = bez cela): celo s rovnakym obsahom ako existujuca
    /// vlastna predloha ju pouzije, inak vznikne nova predloha (este neulozena).
    /// </summary>
    private async Task<(List<(string Id, IReadOnlyDictionary<string, string> Values)?> Boards, List<BoardFace> Created)> PlanFacesAsync(
        StationImportRequest request, CancellationToken cancellationToken)
    {
        var boards = new List<(string Id, IReadOnlyDictionary<string, string> Values)?>(request.Boards.Count);
        var created = new List<BoardFace>();
        if (request.Boards.All(b => b.Face is null))
        {
            boards.AddRange(request.Boards.Select(_ => ((string, IReadOnlyDictionary<string, string>)?)null));
            return (boards, created);
        }

        var known = await KnownFaces.LoadAsync(faces, cancellationToken).ConfigureAwait(false);
        foreach (var board in request.Boards)
        {
            if (board.Face is not { } info || StationFaces.Build(info, board.Flaps is not null, request.FaceLanguage) is not { } generated)
            {
                boards.Add(null);
                continue;
            }

            var content = BoardFaces.Content(generated.Face);
            var id = known.FindByContent(content);
            if (id is null)
            {
                id = known.NewId(generated.IdHint, FacePrefix);
                var face = generated.Face with { Id = id, Name = known.NewName(generated.Face.Name) };
                known.Add(id, face.Name, content);
                created.Add(face);
            }

            boards.Add((id, generated.Values));
        }

        return (boards, created);
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
