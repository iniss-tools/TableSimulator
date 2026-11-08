using TableSimulator.Core.Boards.Announcements;

namespace TableSimulator.Core.Boards.History;

/// <summary>Skoncene hlasenie ustredne alebo vzdialeneho INISSu.</summary>
/// <param name="Line">cislo linky</param>
/// <param name="Address">adresa zariadenia</param>
/// <param name="Announcement">hlasenie s koncom</param>
public sealed record AnnouncementSample(int Line, int Address, Announcement Announcement);
