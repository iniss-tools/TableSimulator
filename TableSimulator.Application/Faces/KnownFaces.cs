using System.Globalization;

namespace TableSimulator.Application.Faces;

/// <summary>
/// Obsadene kluce a nazvy predloh ciel a vlastne predlohy podla obsahu - celo s rovnakym obsahom ako existujuca
/// predloha novu predlohu nezaklada (prevod hlaviciek, cela z dat stanice).
/// </summary>
internal sealed class KnownFaces
{
    private readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _names = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly Dictionary<string, string> _byContent = new(StringComparer.Ordinal);

    /// <summary>Vstavane predlohy (len ich kluce) a vlastne predlohy z databazy.</summary>
    public static async Task<KnownFaces> LoadAsync(IFaceRepository faces, CancellationToken cancellationToken)
    {
        var known = new KnownFaces();
        foreach (var face in BoardFaces.BuiltIn)
        {
            known._ids.Add(face.Id);
            known._names.Add(face.Name);
        }

        foreach (var record in await faces.ListAsync(cancellationToken).ConfigureAwait(false))
            known.Add(record.Id, record.Name, BoardFaces.Content(BoardFaces.Deserialize(record)));
        return known;
    }

    /// <summary>Kluc vlastnej predlohy s rovnakym obsahom; null = taka nie je.</summary>
    public string? FindByContent(string content) => _byContent.GetValueOrDefault(content);

    /// <summary>Zaeviduje novu predlohu.</summary>
    public void Add(string id, string name, string content)
    {
        _ids.Add(id);
        _names.Add(name);
        _byContent.TryAdd(content, id);
    }

    /// <summary>Volny kluc: napoveda (ak je volna), inak napoveda alebo <paramref name="fallback" /> s cislom.</summary>
    public string NewId(string? hint, string fallback)
    {
        var stem = BoardFaces.Slug(hint ?? "", 34);
        if (stem.Length > 0 && !_ids.Contains(stem))
            return stem;
        if (stem.Length == 0)
            stem = fallback;
        for (var n = 1;; n++)
        {
            var id = string.Create(CultureInfo.InvariantCulture, $"{stem}-{n}");
            if (!_ids.Contains(id))
                return id;
        }
    }

    /// <summary>Volny nazov: nazov, alebo nazov s cislom od 2.</summary>
    public string NewName(string name)
    {
        if (!_names.Contains(name))
            return name;
        for (var n = 2;; n++)
        {
            var numbered = string.Create(CultureInfo.InvariantCulture, $"{name} {n}");
            if (!_names.Contains(numbered))
                return numbered;
        }
    }
}
