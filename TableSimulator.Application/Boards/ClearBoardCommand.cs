using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Boards;

/// <summary>Zmaze obsah tabule.</summary>
public sealed record ClearBoardCommand(string Id) : ICommand;
