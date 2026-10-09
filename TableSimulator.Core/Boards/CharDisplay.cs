namespace TableSimulator.Core.Boards;

/// <summary>
/// Znak znakovej tabule: kod (podla kodovania tabule) a cislo pisma; kod -1 = bunka vo vrstve nie je nastavena.
/// </summary>
/// <param name="Code">kod znaku 0-255; -1 = nenastavena</param>
/// <param name="Font">cislo pisma znaku</param>
public readonly record struct CharCell(short Code, byte Font)
{
    /// <summary>Nenastavena bunka (vrstvy striedania nesu len zmenene znaky).</summary>
    public static CharCell Unset { get; } = new(-1, 0);

    /// <summary>Bunka je nastavena.</summary>
    public bool IsSet => Code >= 0;
}

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

/// <summary>
/// Obsah znakovej tabule (Elektrocas LCD, LCD1, slepecke hlasice ERP): riadky zhora od 0, znaky s pismom kazdeho znaku,
/// striedanie vo vrstvach, osvetlenie.
/// </summary>
public sealed class CharDisplay : BoardDisplay
{
    private readonly SortedDictionary<int, CharRow> _rows = [];

    /// <summary>Riadky zhora od 0.</summary>
    public IReadOnlyDictionary<int, CharRow> Rows => _rows;

    /// <summary>Riadok; vytvori ho, ak chyba.</summary>
    public CharRow Row(int row)
    {
        if (!_rows.TryGetValue(row, out var r))
            _rows[row] = r = new CharRow();
        return r;
    }

    /// <summary>Zmaze obsah riadku, ostatne riadky ostanu na mieste.</summary>
    public void ClearRow(int row) => _rows.Remove(row);

    /// <summary>Vlozi prazdny riadok - nizsie riadky sa posunu dole, posledny (<paramref name="rowCount" />) vypadne.</summary>
    public void InsertRow(int row, int rowCount)
    {
        foreach (var r in _rows.Keys.Where(k => k >= row).OrderDescending().ToList())
        {
            var moved = _rows[r];
            _rows.Remove(r);
            if (r + 1 < rowCount)
                _rows[r + 1] = moved;
        }
    }

    /// <summary>Zmaze riadok - nizsie riadky sa posunu hore.</summary>
    public void DeleteRow(int row)
    {
        _rows.Remove(row);
        foreach (var r in _rows.Keys.Where(k => k > row).Order().ToList())
        {
            var moved = _rows[r];
            _rows.Remove(r);
            _rows[r - 1] = moved;
        }
    }

    /// <inheritdoc />
    public override void Clear() => _rows.Clear();
}
