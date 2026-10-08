namespace TableSimulator.Contracts.Signage;

/// <summary>
/// Playlist - spravy po poradi s casom platnosti (POST /api/signage/playlists, PUT /api/signage/playlists/{id}).
/// Spravy sa striedaju od zaciatku casoveho okna, vsetky panely skupiny rovnako.
/// </summary>
/// <param name="Name">nazov</param>
/// <param name="Period">kedy plati</param>
/// <param name="Start">prvy den (pri <see cref="SignagePeriod.Once" /> jediny)</param>
/// <param name="End">posledny den; null = bez konca</param>
/// <param name="Weekdays">dni v tyzdni 1 (pondelok) az 7 (nedela) pri <see cref="SignagePeriod.Weekly" /></param>
/// <param name="From">zaciatok okna v minutach dna (0-1439)</param>
/// <param name="To">koniec okna v minutach dna (0-1440); mensi nez zaciatok = cez polnoc, rovnaky = cely den</param>
/// <param name="Items">spravy po poradi</param>
public sealed record SetSignagePlaylistRequest(
    string Name,
    SignagePeriod Period,
    DateOnly Start,
    DateOnly? End,
    IReadOnlyList<int> Weekdays,
    int From,
    int To,
    IReadOnlyList<SignagePlaylistItemDto> Items);
