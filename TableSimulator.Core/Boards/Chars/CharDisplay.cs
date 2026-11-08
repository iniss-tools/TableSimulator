namespace TableSimulator.Core.Boards.Chars;

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
