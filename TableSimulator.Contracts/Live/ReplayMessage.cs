using TableSimulator.Contracts.Recordings;

namespace TableSimulator.Contracts.Live;

/// <summary>Priebeh prehravania zaznamu.</summary>
public sealed record ReplayMessage(ReplayStatusDto Status) : LiveMessage;
