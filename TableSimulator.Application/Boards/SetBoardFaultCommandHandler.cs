using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class SetBoardFaultCommandHandler(Simulator simulator) : ICommandHandler<SetBoardFaultCommand>
{
    public Task<Result> Handle(SetBoardFaultCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(simulator.SetFault(command.Id, RequestMapper.ToFault(command.Fault))
            ? Result.Success()
            : Result.Failure(BoardErrors.NotFound(command.Id)));
}
