namespace TableSimulator.Contracts.Recordings;

/// <summary>Stav prebiehajuceho zaznamu.</summary>
/// <param name="Entries">pocet zapisanych kusov prevadzky</param>
public sealed record RecorderStatusDto(bool Recording, string? Name, DateTimeOffset? Started, long Entries, long Bytes);
