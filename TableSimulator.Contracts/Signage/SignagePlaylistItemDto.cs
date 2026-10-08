namespace TableSimulator.Contracts.Signage;

/// <summary>Polozka playlistu - ta ista sprava moze byt v playliste viackrat.</summary>
/// <param name="Message">sprava</param>
/// <param name="Duration">trvanie v sekundach; null = trvanie spravy</param>
public sealed record SignagePlaylistItemDto(int Message, int? Duration);
