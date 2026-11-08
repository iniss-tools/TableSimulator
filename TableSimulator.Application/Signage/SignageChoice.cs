using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Co skupina hra: playlist s casom, od ktoreho sa jeho spravy striedaju, predvolena sprava alebo nic.</summary>
/// <param name="Playlist">platny playlist (aj predvoleny); null = nie</param>
/// <param name="Anchor">od kedy sa spravy playlistu striedaju</param>
/// <param name="Message">predvolena sprava, ked nehra ziadny playlist</param>
public readonly record struct SignageChoice(SignagePlaylistDto? Playlist, DateTime Anchor, int? Message);
