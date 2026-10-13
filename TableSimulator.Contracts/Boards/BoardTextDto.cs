namespace TableSimulator.Contracts.Boards;

/// <summary>Co tabula ukazuje ako riadky textu zhora - bez pisma, farieb a jasu.</summary>
/// <param name="Id">identifikator <c>linka-adresa</c></param>
/// <param name="Version">zvysuje sa pri kazdej zmene obsahu alebo nastavenia tabule</param>
/// <param name="PoweredOff">predstierany vypadok napajania - tabula je zhasnuta a riadky su prazdne</param>
/// <param name="Rows">riadky zhora, prazdny = "", striedanie <c>{a|b}</c>, pohyblivy text ELEN posledny ako <c>» text</c></param>
public sealed record BoardTextDto(string Id, long Version, bool PoweredOff, IReadOnlyList<string> Rows);
