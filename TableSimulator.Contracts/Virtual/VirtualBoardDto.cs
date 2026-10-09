namespace TableSimulator.Contracts.Virtual;

/// <summary>
/// Virtualna tabula - co z XML tabule INISSu ukazat na jednej obrazovke (ako definicia tabule v INISSView).
/// </summary>
/// <param name="Id">cislo tabule; adresa zobrazenia <c>#/virtual/&lt;cislo&gt;</c> (ako <c>Tabule.aspx?t=</c>)</param>
/// <param name="Name">nazov</param>
/// <param name="Description">popis pre obsluhu</param>
/// <param name="Source">subor XML tabule bez pripony (<c>SAVE_XML</c> v TPhysic)</param>
/// <param name="RowCount">pocet zaznamov; null = vsetky</param>
/// <param name="RowStart">poradie prveho zaznamu (od 1) - jedna tabula INISSu na viac obrazovkach</param>
/// <param name="Separator">text namiesto <c>#</c> v stanici a smere; prazdny = casti sa striedaju</param>
/// <param name="Template">sablona XSL z INISSView (meno bez pripony); s priecinkom INISSView v nastaveni sa tabula ukaze nou, prazdna = podla rozlozenia</param>
/// <param name="Parameter">atribut <c>x</c> pre sablonu XSL</param>
/// <param name="Layout">rozlozenie; prazdne = predvolene</param>
public sealed record VirtualBoardDto(
    int Id,
    string Name,
    string Description,
    string Source,
    int? RowCount,
    int RowStart,
    string Separator,
    string Template,
    string Parameter,
    string Layout);
