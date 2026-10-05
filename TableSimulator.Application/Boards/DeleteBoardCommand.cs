using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Boards;

/// <summary>Odstrani tabulu.</summary>
public sealed record DeleteBoardCommand(string Id) : ICommand;
