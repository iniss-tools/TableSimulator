using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Application.Virtual;

/// <summary>Odstrani virtualnu tabulu.</summary>
public sealed record DeleteVirtualBoardCommand(int Id) : ICommand;
