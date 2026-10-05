using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class ClearBoardCommandHandler(Simulator simulator) : ICommandHandler<ClearBoardCommand>
{
    public Task<Result> Handle(ClearBoardCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(simulator.ClearBoard(command.Id) ? Result.Success() : Result.Failure(BoardErrors.NotFound(command.Id)));
}
