namespace TableSimulator.Contracts.Archive;

/// <summary>Tabule s rovnakym obsahom v zazname archivu.</summary>
/// <param name="Names">nazvy fyzickych tabul</param>
/// <param name="Columns">stlpce</param>
/// <param name="Logical">logicka tabula (pri informacnom texte)</param>
/// <param name="Row">riadok tabule (pri informacnom texte)</param>
public sealed record TableArchiveTableDto(IReadOnlyList<string> Names, IReadOnlyList<TableArchiveColumnDto> Columns, string Logical, string Row);
