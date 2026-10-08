namespace TableSimulator.Contracts.Signage;

/// <summary>
/// Skupina panelov - vsetky jej panely hraju to iste (POST /api/signage/groups, PUT /api/signage/groups/{id}).
/// Mimo platnosti playlistov hra predvoleny playlist, inak predvolena sprava.
/// </summary>
/// <param name="Name">nazov</param>
/// <param name="Note">poznamka</param>
/// <param name="Playlists">playlisty skupiny</param>
/// <param name="DefaultPlaylist">predvoleny playlist; null = ziadny</param>
/// <param name="DefaultMessage">predvolena sprava (len bez predvoleneho playlistu); null = ziadna</param>
public sealed record SetSignageGroupRequest(string Name, string Note, IReadOnlyList<int> Playlists, int? DefaultPlaylist, int? DefaultMessage);
