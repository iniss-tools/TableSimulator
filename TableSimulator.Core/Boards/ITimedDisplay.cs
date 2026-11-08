using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Boards;

/// <summary>
/// Obsah, ktory sa meni aj bez spravy INISSu - podla casu (snimac tmy, koniec hlasenia).
/// </summary>
public interface ITimedDisplay
{
    /// <summary>Posunie obsah do casu <paramref name="now" />; vrati true, ak sa zmenil.</summary>
    /// <param name="now">aktualny cas</param>
    /// <param name="profile">profil tabule</param>
    /// <param name="location">poloha pre vychod a zapad slnka</param>
    bool Advance(DateTimeOffset now, BoardProfile profile, GeoLocation location);
}
