using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Mapping;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Lines;

public sealed class SetLineCommandHandler(Simulator simulator, TransportManager transport) : ICommandHandler<SetLineCommand>
{
    public async Task<Result> Handle(SetLineCommand command, CancellationToken cancellationToken)
    {
        try
        {
            simulator.SetLine(RequestMapper.ToConfig(command.Request, command.Number));
        }
        catch (ArgumentException e)
        {
            return Result.Failure(LineErrors.Invalid(e.Message));
        }

        await transport.SyncAsync().ConfigureAwait(false);
        return Result.Success();
    }
}
