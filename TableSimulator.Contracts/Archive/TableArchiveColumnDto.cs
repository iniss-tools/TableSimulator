namespace TableSimulator.Contracts.Archive;

/// <summary>Stlpec tabule v zazname archivu - popis stlpca a text (prazdny = stlpec bol prazdny).</summary>
public sealed record TableArchiveColumnDto(string Name, string Value);
