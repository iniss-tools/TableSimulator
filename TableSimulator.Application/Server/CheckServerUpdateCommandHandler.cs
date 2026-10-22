using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Server;

namespace TableSimulator.Application.Server;

public sealed class CheckServerUpdateCommandHandler(IServerUpdates updates) : ICommandHandler<CheckServerUpdateCommand, ServerUpdateDto>
{
    public async Task<Result<ServerUpdateDto>> Handle(CheckServerUpdateCommand command, CancellationToken cancellationToken) =>
        Result.Success(await updates.CheckAsync(cancellationToken).ConfigureAwait(false));
}
