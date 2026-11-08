namespace TableSimulator.Core.Boards.Announcements;

/// <summary>Zariadenie, ktore prehrava hlasenia (ustredna AK Signal, rozhlas vzdialeneho INISSu).</summary>
public interface IAnnouncementDisplay
{
    /// <summary>Posledne hlasenia od najstarsieho vratane prave hrajucich.</summary>
    IReadOnlyList<Announcement> Announcements { get; }
}
