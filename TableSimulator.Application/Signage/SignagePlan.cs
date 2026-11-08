using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Program skupiny: polozky sa striedaju od <paramref name="Anchor" />; vyber sa zmeni v <paramref name="NextChange" />.</summary>
public sealed record SignagePlan(SignagePlaylistDto? Playlist, DateTime Anchor, IReadOnlyList<SignageSlotDto> Items, DateTime NextChange, SignageOffline? Offline)
{
    /// <summary>Sprava, ktora hra v case <paramref name="local" />; null = nic.</summary>
    public SignageMessageDto? PlayingAt(DateTime local) =>
        Items.Count == 0 ? null : Items[SignageSchedule.SlotAt(Items.Select(i => i.Duration).ToList(), Anchor, local)].Message;
}
