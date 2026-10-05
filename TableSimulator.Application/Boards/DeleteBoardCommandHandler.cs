using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class DeleteBoardCommandHandler(Simulator simulator) : ICommandHandler<DeleteBoardCommand>
{
    public Task<Result> Handle(DeleteBoardCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(simulator.RemoveBoard(command.Id) ? Result.Success() : Result.Failure(BoardErrors.NotFound(command.Id)));
}
