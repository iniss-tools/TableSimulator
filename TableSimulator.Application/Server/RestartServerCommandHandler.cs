using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Server;

public sealed class RestartServerCommandHandler(IServerHost host) : ICommandHandler<RestartServerCommand>
{
    public Task<Result> Handle(RestartServerCommand command, CancellationToken cancellationToken)
    {
        if (!host.RunningAsService)
            return Task.FromResult(Result.Failure(ServerErrors.RestartUnavailable));
        host.Restart();
        return Task.FromResult(Result.Success());
    }
}
