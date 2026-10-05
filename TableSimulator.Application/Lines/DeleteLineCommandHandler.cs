using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Lines;

public sealed class DeleteLineCommandHandler(Simulator simulator, TransportManager transport) : ICommandHandler<DeleteLineCommand>
{
    public async Task<Result> Handle(DeleteLineCommand command, CancellationToken cancellationToken)
    {
        if (!simulator.RemoveLine(command.Number))
            return Result.Failure(LineErrors.NotFound(command.Number));
        await transport.SyncAsync().ConfigureAwait(false);
        return Result.Success();
    }
}
