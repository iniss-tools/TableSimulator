namespace TableSimulator.Core.Virtual;

/// <summary>
/// Polozka zaznamu tabule v XML INISSu - prvok s menom podla druhu obsahu stlpca (<c>time</c>, <c>number</c>,
/// <c>destination</c>…) a textom, ktory by INISS poslal na tabulu (vratane striedanych casti oddelenych <c>#</c>).
/// </summary>
public sealed record XmlTableCell(string Name, string Text);

/// <summary>Zaznam tabule (prvok <c>row</c>) - polozky v poradi stlpcov katalogovej tabule.</summary>
public sealed record XmlTableRow(IReadOnlyList<XmlTableCell> Cells)
{
    /// <summary>Text prvej polozky s menom <paramref name="name" />; null, ak v zazname nie je.</summary>
    public string? Text(string name) => Cells.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))?.Text;
}

/// <summary>
/// Obsah tabule, ktory INISS zapisuje do <c>&lt;SAVE_XML&gt;.xml</c> v priecinku logov (koren <c>station</c>).
/// Casy su miestny cas pocitaca s INISSom.
/// </summary>
/// <param name="Station">nazov stanice (atribut <c>name</c>)</param>
/// <param name="Key">kluc tabule z TPhysic (atribut <c>n</c>)</param>
/// <param name="Written">cas zapisu (atribut <c>date</c>)</param>
/// <param name="ValidTo">koniec platnosti obsahu (atribut <c>validto</c>) - potom INISS asi nebezi</param>
/// <param name="Software">verzia INISSu (atribut <c>sw</c>)</param>
/// <param name="Rows">zaznamy tabule</param>
public sealed record XmlTable(string Station, string Key, DateTime? Written, DateTime? ValidTo, string Software, IReadOnlyList<XmlTableRow> Rows)
{
    /// <summary>Obsah uz nie je platny - INISS subor v case platnosti neobnovil.</summary>
    public bool IsStale(DateTime now) => ValidTo is { } validTo && validTo < now;
}
