using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Lines;
using TableSimulator.Application.Mapping;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Boards;

public sealed class SetBoardCommandHandler(Simulator simulator) : ICommandHandler<SetBoardCommand>
{
    public Task<Result> Handle(SetBoardCommand command, CancellationToken cancellationToken)
    {
        if (simulator.Read(s => s.FindLine(command.Line)) is null)
            return Task.FromResult(Result.Failure(LineErrors.NotFound(command.Line)));

        try
        {
            simulator.SetBoard(RequestMapper.ToConfig(command.Request, command.Line, command.Address));
            return Task.FromResult(Result.Success());
        }
        catch (ArgumentException e)
        {
            return Task.FromResult(Result.Failure(BoardErrors.Invalid(e.Message)));
        }
    }
}
