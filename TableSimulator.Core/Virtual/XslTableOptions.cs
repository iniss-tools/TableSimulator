namespace TableSimulator.Core.Virtual;

/// <summary>Co z XML tabule ukazat (ako parametre 5 az 7 definicie tabule INISSView).</summary>
/// <param name="RowCount">pocet zaznamov; null = vsetky</param>
/// <param name="RowStart">poradie prveho zaznamu (od 1)</param>
/// <param name="Separator">text namiesto <c>#</c> v stanici a smere; prazdny = casti sa striedaju (skriptom stranky)</param>
/// <param name="Parameter">atribut <c>x</c> korena pre sablonu; prazdny = bez neho</param>
public sealed record XslTableOptions(int? RowCount, int RowStart, string Separator, string Parameter);
