namespace TableSimulator.Application.Signage;

/// <summary>Vsetko o informacnych paneloch naraz (je toho malo).</summary>
public sealed record SignageRecords(
    IReadOnlyList<SignageMessageRecord> Messages,
    IReadOnlyList<SignagePlaylistRecord> Playlists,
    IReadOnlyList<SignageGroupRecord> Groups,
    IReadOnlyList<SignagePanelRecord> Panels,
    IReadOnlyList<SignageMediaRecord> Media);
