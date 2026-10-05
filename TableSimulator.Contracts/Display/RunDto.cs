namespace TableSimulator.Contracts.Display;

/// <summary>Usek textu ELEN - bajty ako cisla (nie base64), aby ich prehliadac priamo pouzil ako indexy znakov pisma.</summary>
/// <param name="X">poloha kurzora v bodoch; null = za predchadzajucim usekom</param>
public sealed record RunDto(int? X, int Font, int[] Codes, string Text);
