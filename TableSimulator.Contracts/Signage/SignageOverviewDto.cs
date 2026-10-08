namespace TableSimulator.Contracts.Signage;

/// <summary>Spravy, playlisty, skupiny a panely (GET /api/signage).</summary>
public sealed record SignageOverviewDto(
    IReadOnlyList<SignageMessageDto> Messages,
    IReadOnlyList<SignagePlaylistDto> Playlists,
    IReadOnlyList<SignageGroupDto> Groups,
    IReadOnlyList<SignagePanelDto> Panels);
