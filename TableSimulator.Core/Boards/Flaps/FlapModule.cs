namespace TableSimulator.Core.Boards.Flaps;

/// <summary>
/// Modul listovej tabule.
/// </summary>
/// <param name="Line">riadok v ramci zaznamu (od 0)</param>
/// <param name="Position">prva pozicia v riadku (znak spravy INISSu)</param>
/// <param name="Span">pocet pozicii - ich kody spolu urcuju list</param>
/// <param name="List">meno zoznamu listov; null = ukaze sa kod</param>
public sealed record FlapModule(int Line, int Position, int Span, string? List);
