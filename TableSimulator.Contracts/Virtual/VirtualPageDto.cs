namespace TableSimulator.Contracts.Virtual;

/// <summary>
/// Stranka virtualnej tabule zo sablony XSL INISSView: hotove HTML (relativne adresy smeruju do priecinka INISSView
/// cez <c>/inissview/app/</c>) alebo dovod, preco ju nebolo mozne zostavit.
/// </summary>
/// <param name="Html">stranka; null pri chybe</param>
/// <param name="Error">kod chyby: <c>noSource</c> (XML tabule nie je), <c>notFound</c> (sablona nie je), <c>template</c> (chybna sablona), <c>failed</c> (XML sa neda spracovat)</param>
/// <param name="Detail">podrobnost chyby (cesta, sprava)</param>
public sealed record VirtualPageDto(string? Html, string? Error, string? Detail);
