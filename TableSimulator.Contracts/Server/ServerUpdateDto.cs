namespace TableSimulator.Contracts.Server;

/// <summary>Nova verzia servera podla posledneho vydania na GitHube.</summary>
/// <param name="Release">server je zostava z vydania - len ta zistuje nove verzie</param>
/// <param name="Version">verzia servera</param>
/// <param name="Latest">novsia verzia z vydania; null = ziadna alebo sa este nezistovala</param>
/// <param name="Page">stranka vydania novsej verzie</param>
/// <param name="Installer">adresa instalatora novsej verzie</param>
/// <param name="Notes">co je nove v novsej verzii (Markdown)</param>
/// <param name="CheckedAt">kedy sa naposledy zistovalo</param>
/// <param name="Error">chyba posledneho zistovania (siet, index); null = bez chyby</param>
public sealed record ServerUpdateDto(
    bool Release,
    string Version,
    string? Latest,
    string? Page,
    string? Installer,
    string? Notes,
    DateTimeOffset? CheckedAt,
    string? Error);
