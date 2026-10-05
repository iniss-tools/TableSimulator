using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

/// <summary>Zalozi alebo zmeni virtualnu tabulu s cislom <paramref name="Id" />.</summary>
public sealed record SetVirtualBoardCommand(int Id, SetVirtualBoardRequest Request) : ICommand;
