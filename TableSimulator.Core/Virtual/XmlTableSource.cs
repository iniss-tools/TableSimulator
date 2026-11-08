namespace TableSimulator.Core.Virtual;

/// <summary>Subor XML tabule najdeny v sledovanych priecinkoch.</summary>
/// <param name="Name">meno bez pripony (hodnota <c>SAVE_XML</c> v TPhysic)</param>
/// <param name="Path">cesta k suboru, z ktoreho je obsah</param>
/// <param name="Modified">cas poslednej zmeny suboru (miestny)</param>
/// <param name="Table">posledny uspesne precitany obsah; null, ak sa este nepodarilo precitat</param>
/// <param name="Error">preco sa subor opakovane neda precitat; null = v poriadku</param>
public sealed record XmlTableSource(string Name, string Path, DateTime Modified, XmlTable? Table, string? Error)
{
    /// <summary>Obsah uz nie je platny (INISS asi nebezi).</summary>
    public bool IsStale(DateTime now) => Table?.IsStale(now) ?? false;
}
