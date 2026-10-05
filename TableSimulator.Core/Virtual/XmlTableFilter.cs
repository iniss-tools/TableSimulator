namespace TableSimulator.Core.Virtual;

/// <summary>
/// Co z XML tabule ukaze jedna virtualna tabula - rovnako ako INISSView: vyber zaznamov (pocet od poradia,
/// tak sa jedna tabula INISSu rozdeli na viac monitorov) a nahrada znaku <c>#</c> medzi striedanymi castami
/// textu oddelovacom, aby sa casti zobrazili naraz.
/// </summary>
public static class XmlTableFilter
{
    /// <summary>Znak medzi striedanymi castami textu; text, ktory nim konci, blika.</summary>
    public const char Alternation = '#';

    // INISSView meni oddelovac len v stanici a smere - cislo, cas ani poznamka sa nespajaju
    private static readonly string[] SeparatorCells = ["destination", "direction"];

    /// <summary>Zaznamy pre virtualnu tabulu.</summary>
    /// <param name="rows">vsetky zaznamy z XML</param>
    /// <param name="count">pocet zaznamov; null = vsetky od <paramref name="start" /></param>
    /// <param name="start">poradie prveho zaznamu (od 1)</param>
    /// <param name="separator">text namiesto <c>#</c>; prazdny = casti sa striedaju</param>
    public static IReadOnlyList<XmlTableRow> Apply(IReadOnlyList<XmlTableRow> rows, int? count, int start, string? separator)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var selected = rows.Skip(Math.Max(start, 1) - 1);
        if (count is > 0)
            selected = selected.Take(count.Value);
        return string.IsNullOrEmpty(separator)
            ? selected.ToArray()
            : selected.Select(r => new XmlTableRow(r.Cells.Select(c => Substitute(c, separator)).ToArray())).ToArray();
    }

    // text konciaci znakom # blika - ten ostava, ako je
    private static XmlTableCell Substitute(XmlTableCell cell, string separator) =>
        SeparatorCells.Contains(cell.Name, StringComparer.OrdinalIgnoreCase) && cell.Text.Length > 0 && cell.Text[^1] != Alternation
            ? cell with { Text = cell.Text.Replace(Alternation.ToString(), separator, StringComparison.Ordinal) }
            : cell;
}
