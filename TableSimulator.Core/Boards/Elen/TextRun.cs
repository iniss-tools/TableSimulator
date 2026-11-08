namespace TableSimulator.Core.Boards.Elen;

/// <summary>
/// Usek textu jednym pismom - tabula ho kresli od polohy <see cref="X" /> alebo za predchadzajuci usek.
/// </summary>
/// <param name="X">poloha v bodoch; null = pokracuje za predchadzajucim usekom</param>
/// <param name="Font">cislo pisma (dolny bajt, pri rozsirenom pisme aj horny)</param>
/// <param name="Codes">bajty textu tak, ako prisli tabuli (podla nich sa beru znaky pisma)</param>
/// <param name="Text">text dekodovany podla kodovania variantu</param>
public sealed record TextRun(int? X, int Font, byte[] Codes, string Text);
