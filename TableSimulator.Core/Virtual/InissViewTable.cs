namespace TableSimulator.Core.Virtual;

/// <summary>Definicia tabule INISSView - jeden kluc s cislom v <c>appSettings</c> suboru <c>Web.config</c>.</summary>
/// <param name="Number">cislo tabule (<c>Tabule.aspx?t=</c>)</param>
/// <param name="Name">parameter 1 - kluc tabule z TPhysic</param>
/// <param name="Title">parameter 2 - popis pre zoznam (predvolene nazov)</param>
/// <param name="Xml">parameter 3 - subor XML bez pripony (predvolene nazov)</param>
/// <param name="Template">parameter 4 - sablona XSL bez pripony (predvolene nazov)</param>
/// <param name="RowCount">parameter 5 pred lomkou - pocet zaznamov; null = vsetky</param>
/// <param name="RowStart">parameter 5 za lomkou - poradie prveho zaznamu (od 1)</param>
/// <param name="Separator">parameter 6 - text namiesto <c>#</c>; prazdny = casti sa striedaju</param>
/// <param name="Parameter">parameter 7 - atribut <c>x</c> pre sablonu</param>
public sealed record InissViewTable(int Number, string Name, string Title, string Xml, string Template, int? RowCount, int RowStart, string Separator, string Parameter);
