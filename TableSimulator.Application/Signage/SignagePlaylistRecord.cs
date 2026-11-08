namespace TableSimulator.Application.Signage;

/// <summary>Playlist v databaze; obsah (<see cref="Contracts.Signage.SetSignagePlaylistRequest" />) ako JSON.</summary>
public sealed record SignagePlaylistRecord(int Id, string Name, string Definition);
