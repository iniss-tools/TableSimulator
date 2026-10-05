using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Replay;

/// <summary>Zastavi prehravanie zaznamu.</summary>
public sealed record StopReplayCommand : ICommand;
