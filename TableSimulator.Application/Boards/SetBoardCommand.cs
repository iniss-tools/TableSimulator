using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Prida tabulu alebo zmeni tabulu s linkou a adresou z adresy poziadavky.</summary>
public sealed record SetBoardCommand(int Line, int Address, SetBoardRequest Request) : ICommand;
