namespace TableSimulator.Contracts.Virtual;

/// <summary>Vysledok nacitania definicii z INISSView.</summary>
/// <param name="Added">nove tabule</param>
/// <param name="Replaced">prepisane tabule s rovnakym cislom</param>
/// <param name="Skipped">tabule s cislom, ktore uz bolo (bez prepisania)</param>
/// <param name="Duplicates">cisla, ktore Web.config definuje viackrat (plati prva definicia)</param>
/// <param name="Folders">priecinky s XML tabul z Web.config (<c>TabXmlDir</c>)</param>
public sealed record InissViewImportedDto(int Added, int Replaced, int Skipped, IReadOnlyList<int> Duplicates, IReadOnlyList<string> Folders);
