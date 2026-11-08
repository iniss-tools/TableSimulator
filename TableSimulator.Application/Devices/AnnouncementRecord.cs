namespace TableSimulator.Application.Devices;

/// <summary>
/// Skoncene hlasenie ustredne AK Signal alebo vzdialeneho INISSu v databaze; zvuky oddelene <c>*</c> ako ich posiela
/// server, vysledok klucom (<c>completed</c>, <c>stopped</c>, <c>cancelled</c>); fronta a okruh len pri vzdialenom INISSe.
/// </summary>
public sealed record AnnouncementRecord(
    long Id,
    int Line,
    int Address,
    int Channel,
    long StartMs,
    long EndMs,
    string Result,
    string Sounds,
    int? Value,
    string? Title,
    string? Queue = null,
    string? Circuit = null);
