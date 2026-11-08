namespace TableSimulator.Core.Archive;

/// <summary>
/// Skupina fyzickych tabul s rovnakym obsahom: ich nazvy a stlpce. Pri zazname informacneho textu ma logicku tabulu
/// (<c>lt</c>) a riadok (<c>r</c>) a stlpce nema.
/// </summary>
public sealed record TableArchiveTable(IReadOnlyList<string> Names, IReadOnlyList<TableArchiveColumn> Columns, string Logical, string Row);
