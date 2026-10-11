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

        var known = await KnownFaces.LoadAsync(faces, cancellationToken).ConfigureAwait(false);
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
        var known = await KnownFaces.LoadAsync(faces, cancellationToken).ConfigureAwait(false);
        return await AbsorbAsync(board, known, name, idHint, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BoardConfig> AbsorbAsync(BoardConfig board, KnownFaces known, string? name, string? idHint, CancellationToken cancellationToken)
    {
        if (board.Profile.Header is not { } header)
            return board;
        // tabula, ktora uz celo ma, hlavicku len zahodi
        if (board.Profile.Face is not null)
            return board with { Profile = board.Profile with { Header = null } };

        var face = BoardFaces.FromHeader(header, "", "");
        var content = BoardFaces.Content(face);
        var id = known.FindByContent(content);
        if (id is null)
        {
            id = known.NewId(idHint, HeaderPrefix);
            var title = !string.IsNullOrWhiteSpace(name) ? name.Trim()
                : !string.IsNullOrWhiteSpace(board.Name) ? board.Name.Trim()
                : string.Create(CultureInfo.InvariantCulture, $"{board.Line}-{board.Address}");
            face = face with { Id = id, Name = title.Length > 100 ? title[..100] : title };
            await faces.SaveAsync(new FaceRecord(id, face.Name, BoardFaces.Serialize(face)), cancellationToken).ConfigureAwait(false);
            known.Add(id, face.Name, content);
        }

        return board with { Profile = board.Profile with { Header = null, Face = id } };
    }
}
