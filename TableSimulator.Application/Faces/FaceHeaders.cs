using System.Globalization;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Faces;

/// <summary>
/// Prevod starsich hlaviciek tabul (<see cref="BoardProfile.Header" />) na predlohy ciel: tabula s hlavickou dostane
/// predlohu s rovnakym obsahom - existujucu, ak uz taka je, inak novu vlastnu. Pouziva sa pri starte servera, ulozeni
/// tabule, importe konfiguracie a importe z ELEN Monitora.
/// </summary>
public sealed class FaceHeaders(IFaceRepository faces)
{
    /// <summary>Kluc novej predlohy z hlavicky bez napovedy: <c>hlavicka-1</c>, <c>hlavicka-2</c>…</summary>
    private const string HeaderPrefix = "hlavicka";

    /// <summary>Tabule konfiguracie bez hlaviciek; changed = niektora hlavicka sa previedla.</summary>
    public async Task<(SimulatorConfig Config, bool Changed)> AbsorbAsync(SimulatorConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Boards.All(b => b.Profile.Header is null))
            return (config, false);

        var known = await KnownAsync(cancellationToken).ConfigureAwait(false);
        var boards = new List<BoardConfig>(config.Boards.Count);
        foreach (var board in config.Boards)
            boards.Add(await AbsorbAsync(board, known, null, null, cancellationToken).ConfigureAwait(false));
        return (config with { Boards = boards }, true);
    }

    /// <summary>
    /// Tabula bez hlavicky; nova predloha z jej hlavicky dostane nazov <paramref name="name" /> (inak nazov tabule)
    /// a kluc z <paramref name="idHint" /> (inak <c>hlavicka-n</c>).
    /// </summary>
    public async Task<BoardConfig> AbsorbAsync(BoardConfig board, string? name, string? idHint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Profile.Header is null)
            return board;
        var known = await KnownAsync(cancellationToken).ConfigureAwait(false);
        return await AbsorbAsync(board, known, name, idHint, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BoardConfig> AbsorbAsync(BoardConfig board, Known known, string? name, string? idHint, CancellationToken cancellationToken)
    {
        if (board.Profile.Header is not { } header)
            return board;
        // tabula, ktora uz celo ma, hlavicku len zahodi
        if (board.Profile.Face is not null)
            return board with { Profile = board.Profile with { Header = null } };

        var face = BoardFaces.FromHeader(header, "", "");
        var content = BoardFaces.Content(face);
        if (!known.ByContent.TryGetValue(content, out var id))
        {
            id = known.NewId(idHint);
            var title = !string.IsNullOrWhiteSpace(name) ? name.Trim()
                : !string.IsNullOrWhiteSpace(board.Name) ? board.Name.Trim()
                : string.Create(CultureInfo.InvariantCulture, $"{board.Line}-{board.Address}");
            face = face with { Id = id, Name = title.Length > 100 ? title[..100] : title };
            await faces.SaveAsync(new FaceRecord(id, face.Name, BoardFaces.Serialize(face)), cancellationToken).ConfigureAwait(false);
            known.Add(id, content);
        }

        return board with { Profile = board.Profile with { Header = null, Face = id } };
    }

    private async Task<Known> KnownAsync(CancellationToken cancellationToken)
    {
        var known = new Known();
        foreach (var face in BoardFaces.BuiltIn)
            known.Ids.Add(face.Id);
        foreach (var record in await faces.ListAsync(cancellationToken).ConfigureAwait(false))
            known.Add(record.Id, BoardFaces.Content(BoardFaces.Deserialize(record)));
        return known;
    }

    /// <summary>Obsadene kluce a vlastne predlohy podla obsahu.</summary>
    private sealed class Known
    {
        public HashSet<string> Ids { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> ByContent { get; } = new(StringComparer.Ordinal);

        public void Add(string id, string content)
        {
            Ids.Add(id);
            ByContent.TryAdd(content, id);
        }

        /// <summary>Volny kluc: napoveda (ak je volna), inak napoveda alebo <c>hlavicka</c> s cislom.</summary>
        public string NewId(string? hint)
        {
            var stem = BoardFaces.Slug(hint ?? "", 34);
            if (stem.Length > 0 && !Ids.Contains(stem))
                return stem;
            if (stem.Length == 0)
                stem = HeaderPrefix;
            for (var n = 1;; n++)
            {
                var id = string.Create(CultureInfo.InvariantCulture, $"{stem}-{n}");
                if (!Ids.Contains(id))
                    return id;
            }
        }
    }
}
