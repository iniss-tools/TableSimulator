using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Application.Frames;

public sealed class ClearLogCommandHandler(Simulator simulator) : ICommandHandler<ClearLogCommand>
{
    public Task<Result> Handle(ClearLogCommand command, CancellationToken cancellationToken)
    {
        simulator.ClearLog();
        return Task.FromResult(Result.Success());
    }
}
