namespace TableSimulator.Core.Boards.Chars;

/// <summary>
/// Riadok znakovej tabule vo vrstvach: vrstva 0 je zakladny text, vrstvy 1, 2… su dalsie casti striedania, ktore nesu
/// len znaky odlisne od vrstvy 0.
/// </summary>
public sealed class CharRow
{
    private readonly SortedDictionary<int, CharCell[]> _layers = [];

    /// <summary>Vrstvy podla cisla.</summary>
    public IReadOnlyDictionary<int, CharCell[]> Layers => _layers;

    /// <summary>Riadok je prazdny.</summary>
    public bool IsEmpty => _layers.Count == 0;

    /// <summary>
    /// Zapise znaky od stlpca <paramref name="column" /> do vrstvy; zapis do vrstvy 0 v tom istom useku zrusi vyssie vrstvy
    /// (INISS posiela striedanie vzdy znova po zakladnom texte).
    /// </summary>
    public void Write(int layer, int column, ReadOnlySpan<byte> codes, ReadOnlySpan<byte> fonts)
    {
        var cells = Layer(layer, column + codes.Length);
        for (var i = 0; i < codes.Length; i++)
            cells[column + i] = new CharCell(codes[i], fonts[Math.Min(i, fonts.Length - 1)]);

        if (layer == 0)
        {
            foreach (var (number, other) in _layers.Where(l => l.Key > 0).ToList())
            {
                for (var i = column; i < Math.Min(other.Length, column + codes.Length); i++)
                    other[i] = CharCell.Unset;
                if (other.All(c => !c.IsSet))
                    _layers.Remove(number);
            }
        }
    }

    private CharCell[] Layer(int layer, int length)
    {
        if (!_layers.TryGetValue(layer, out var cells))
        {
            cells = new CharCell[length];
            Array.Fill(cells, CharCell.Unset);
        }
        else if (cells.Length < length)
        {
            var bigger = new CharCell[length];
            Array.Fill(bigger, CharCell.Unset);
            cells.CopyTo(bigger, 0);
            cells = bigger;
        }

        _layers[layer] = cells;
        return cells;
    }
}
