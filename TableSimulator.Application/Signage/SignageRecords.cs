namespace TableSimulator.Application.Signage;

/// <summary>Sprava v databaze; obsah (<see cref="Contracts.Signage.SetSignageMessageRequest" />) ako JSON.</summary>
public sealed record SignageMessageRecord(int Id, string Name, string Definition);

/// <summary>Playlist v databaze; obsah (<see cref="Contracts.Signage.SetSignagePlaylistRequest" />) ako JSON.</summary>
public sealed record SignagePlaylistRecord(int Id, string Name, string Definition);

/// <summary>Skupina panelov v databaze; obsah (<see cref="Contracts.Signage.SetSignageGroupRequest" />) ako JSON.</summary>
public sealed record SignageGroupRecord(int Id, string Name, string Definition);

/// <summary>Panel v databaze.</summary>
public sealed record SignagePanelRecord(int Id, string Name, string Note, int? GroupId);

/// <summary>Nahraty subor spravy; obsah je v priecinku <c>media</c> pod menom <paramref name="FileName" />.</summary>
public sealed record SignageMediaRecord(Guid Id, string Name, string ContentType, long Size, string FileName, DateTimeOffset CreatedAt);

/// <summary>Vsetko o informacnych paneloch naraz (je toho malo).</summary>
public sealed record SignageRecords(
    IReadOnlyList<SignageMessageRecord> Messages,
    IReadOnlyList<SignagePlaylistRecord> Playlists,
    IReadOnlyList<SignageGroupRecord> Groups,
    IReadOnlyList<SignagePanelRecord> Panels,
    IReadOnlyList<SignageMediaRecord> Media);

/// <summary>Co sa maze.</summary>
public enum SignageEntity
{
    Message,
    Playlist,
    Group,
    Panel
}
