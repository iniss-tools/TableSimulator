namespace TableSimulator.Core.Virtual;

/// <summary>Zaznam tabule (prvok <c>row</c>) - polozky v poradi stlpcov katalogovej tabule.</summary>
public sealed record XmlTableRow(IReadOnlyList<XmlTableCell> Cells)
{
    /// <summary>Text prvej polozky s menom <paramref name="name" />; null, ak v zazname nie je.</summary>
    public string? Text(string name) => Cells.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Text;
}
