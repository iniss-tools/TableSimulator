using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Replay;

/// <summary>Zastavi prehravanie zaznamu.</summary>
public sealed record StopReplayCommand : ICommand;
