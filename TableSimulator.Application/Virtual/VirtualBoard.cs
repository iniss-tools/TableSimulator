namespace TableSimulator.Application.Virtual;

/// <summary>
/// Definicia virtualnej tabule ulozena v databaze - co z XML tabule INISSu ukazat na jednej obrazovke
/// (zodpoveda definicii tabule vo <c>Web.config</c> INISSView).
/// </summary>
/// <param name="Id">cislo tabule (<c>#/virtual/&lt;cislo&gt;</c>)</param>
/// <param name="Name">nazov</param>
/// <param name="Description">popis pre obsluhu</param>
/// <param name="Source">subor XML bez pripony (<c>SAVE_XML</c>)</param>
/// <param name="RowCount">pocet zaznamov; null = vsetky</param>
/// <param name="RowStart">poradie prveho zaznamu (od 1)</param>
/// <param name="Separator">text namiesto <c>#</c>; prazdny = casti sa striedaju</param>
/// <param name="Template">sablona XSL z INISSView (zatial sa nepouziva)</param>
/// <param name="Parameter">atribut <c>x</c> pre sablonu (zatial sa nepouziva)</param>
/// <param name="Layout">rozlozenie; prazdne = predvolene</param>
public sealed record VirtualBoard(
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
