using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Spravy, playlisty, skupiny a panely rozbalene z databazy.</summary>
public sealed class SignageData
{
    private SignageData(SignageRecords records)
    {
        Media = records.Media.ToDictionary(m => m.Id);
        Messages = records.Messages.ToDictionary(m => m.Id, m =>
        {
            var definition = SignageJson.Deserialize<SetSignageMessageRequest>(m.Definition);
            return new SignageMessageDto(m.Id, definition, definition.Media is { } media && Media.TryGetValue(media, out var file) ? file.Name : null);
        });
        Playlists = records.Playlists.ToDictionary(p => p.Id, p => new SignagePlaylistDto(p.Id, SignageJson.Deserialize<SetSignagePlaylistRequest>(p.Definition)));
        Groups = records.Groups.ToDictionary(g => g.Id, g => new SignageGroupDto(g.Id, SignageJson.Deserialize<SetSignageGroupRequest>(g.Definition)));
        Panels = records.Panels.ToDictionary(p => p.Id);
    }

    public IReadOnlyDictionary<int, SignageMessageDto> Messages { get; }
    public IReadOnlyDictionary<int, SignagePlaylistDto> Playlists { get; }
    public IReadOnlyDictionary<int, SignageGroupDto> Groups { get; }
    public IReadOnlyDictionary<int, SignagePanelRecord> Panels { get; }
    public IReadOnlyDictionary<Guid, SignageMediaRecord> Media { get; }

    public static SignageData From(SignageRecords records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return new SignageData(records);
    }

    /// <summary>Playlisty, v ktorych je sprava.</summary>
    public IEnumerable<SignagePlaylistDto> PlaylistsWith(int message) => Playlists.Values.Where(p => p.Definition.Items.Any(i => i.Message == message));

    /// <summary>Program skupiny v case <paramref name="local" /> (miestny cas servera).</summary>
    public SignagePlan PlanFor(SignageGroupDto group, DateTime local)
    {
        ArgumentNullException.ThrowIfNull(group);
        var playlists = group.Definition.Playlists.Select(id => Playlists.GetValueOrDefault(id)).OfType<SignagePlaylistDto>().ToList();
        var fallback = group.Definition.DefaultPlaylist is { } d ? Playlists.GetValueOrDefault(d) : null;
        var message = group.Definition.DefaultMessage is { } m && Messages.ContainsKey(m) ? m : (int?)null;
        var choice = SignageSchedule.Select(playlists, fallback, message, local);
        var next = SignageSchedule.NextChange(playlists, fallback, message, local);
        if (choice.Playlist is { } playlist)
        {
            var items = playlist.Definition.Items
                .Where(i => Messages.ContainsKey(i.Message))
                .Select(i => new SignageSlotDto(Messages[i.Message], i.Duration ?? Messages[i.Message].Definition.Duration))
                .ToList();
            return new SignagePlan(playlist, choice.Anchor, items, next, items.Count == 0 ? SignageOffline.Empty : null);
        }

        return choice.Message is { } single
            ? new SignagePlan(null, choice.Anchor, [new SignageSlotDto(Messages[single], Messages[single].Definition.Duration)], next, null)
            : new SignagePlan(null, choice.Anchor, [], next, SignageOffline.NoProgram);
    }
}

/// <summary>Program skupiny: polozky sa striedaju od <paramref name="Anchor" />; vyber sa zmeni v <paramref name="NextChange" />.</summary>
public sealed record SignagePlan(SignagePlaylistDto? Playlist, DateTime Anchor, IReadOnlyList<SignageSlotDto> Items, DateTime NextChange, SignageOffline? Offline)
{
    /// <summary>Sprava, ktora hra v case <paramref name="local" />; null = nic.</summary>
    public SignageMessageDto? PlayingAt(DateTime local) =>
        Items.Count == 0 ? null : Items[SignageSchedule.SlotAt(Items.Select(i => i.Duration).ToList(), Anchor, local)].Message;
}
