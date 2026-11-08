namespace TableSimulator.Core.Archive;

/// <summary>Stlpec tabule v zazname - popis stlpca z katalogovej tabule a text (prazdny, ak bol stlpec prazdny).</summary>
public sealed record TableArchiveColumn(string Name, string Value);
