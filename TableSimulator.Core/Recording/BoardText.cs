using TableSimulator.Core.Boards;
using TableSimulator.Core.Protocols.Elektrocas;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Co tabula ukazuje ako riadky textu zhora (na porovnanie zaznamov): kazdy riadok profilu, prazdny riadok = "",
/// striedanie ako <c>{a|b}</c>; pohyblivy text ELEN ako posledny riadok <c>» text</c>. Pismo, farby a jas sa
/// neporovnavaju.
/// </summary>
public static class BoardText
{
    /// <summary>Predpona riadku s pohyblivym textom.</summary>
    public const string MovingPrefix = "» ";

    public static IReadOnlyList<string> Rows(SimBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Display switch
        {
            ElenDisplay elen => Elen(elen, board.Profile.Rows),
            CharDisplay chars => Chars(chars, board.Profile.Rows, board.ManufacturerKey),
            _ => []
        };
    }

    private static List<string> Elen(ElenDisplay display, int rows)
    {
        // riadky ELEN su cislovane odspodu - zhora je najvyssie cislo
        var top = Math.Max(rows, display.Rows.Count == 0 ? 0 : display.Rows.Keys.Max());
        var text = new List<string>();
        for (var row = top; row >= 1; row--)
            text.Add(display.Rows.TryGetValue(row, out var content) ? content.PlainText.TrimEnd() : "");
        if (display.MovingText is { Length: > 0 } moving)
            text.Add(MovingPrefix + moving.Trim());
        return text;
    }

    private static List<string> Chars(CharDisplay display, int rows, string manufacturer)
    {
        var count = Math.Max(rows, display.Rows.Count == 0 ? 0 : display.Rows.Keys.Max() + 1);
        var text = new List<string>();
        for (var row = 0; row < count; row++)
            text.Add(display.Rows.TryGetValue(row, out var content) ? Row(content, manufacturer) : "");
        return text;
    }

    /// <summary>Riadok znakovej tabule: zakladna vrstva, dalsie vrstvy striedania prekryte cez nu ako <c>{a|b}</c>.</summary>
    private static string Row(CharRow row, string manufacturer)
    {
        if (row.IsEmpty)
            return "";
        var length = row.Layers.Values.Max(l => l.Length);
        var base0 = row.Layers.TryGetValue(0, out var first) ? first : [];
        var parts = row.Layers.Keys.Order().Select(layer =>
        {
            var codes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                var cell = i < row.Layers[layer].Length && row.Layers[layer][i].IsSet ? row.Layers[layer][i]
                    : i < base0.Length && base0[i].IsSet ? base0[i] : new CharCell(0x20, 0);
                codes[i] = (byte)cell.Code;
            }

            return ElektrocasCharset.Decode(codes, manufacturer).TrimEnd();
        }).Distinct(StringComparer.Ordinal).ToList();
        return parts.Count == 1 ? parts[0] : "{" + string.Join("|", parts) + "}";
    }
}
