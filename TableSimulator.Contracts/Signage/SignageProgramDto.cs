namespace TableSimulator.Contracts.Signage;

/// <summary>
/// Co ma panel hrat (GET /api/signage/panels/{id}/program): polozky sa striedaju od <paramref name="Anchor" /> dokola,
/// panel si poradie pocita sam podla <paramref name="Now" /> servera a program si vypyta znova najneskor
/// v <paramref name="ValidUntil" /> (alebo po zmene na serveri).
/// </summary>
/// <param name="Now">cas servera</param>
/// <param name="Panel">nazov panelu</param>
/// <param name="Group">skupina; null = panel nema skupinu</param>
/// <param name="Playlist">hrajuci playlist; null = predvolena sprava alebo nic</param>
/// <param name="Anchor">od kedy sa polozky striedaju</param>
/// <param name="Items">polozky po poradi; prazdne = mimo prevadzky</param>
/// <param name="ValidUntil">kedy sa vyber playlistu zmeni</param>
/// <param name="Offline">preco panel nic nehra; null = hra</param>
public sealed record SignageProgramDto(
    DateTimeOffset Now,
    string Panel,
    SignageGroupDto? Group,
    SignagePlaylistDto? Playlist,
    DateTimeOffset Anchor,
    IReadOnlyList<SignageSlotDto> Items,
    DateTimeOffset ValidUntil,
    SignageOffline? Offline);
