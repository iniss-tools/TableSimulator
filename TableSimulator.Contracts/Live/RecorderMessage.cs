using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Contracts.Live;

/// <summary>Stav prebiehajuceho zaznamu.</summary>
public sealed record RecorderMessage(RecorderStatusDto Status) : LiveMessage;
