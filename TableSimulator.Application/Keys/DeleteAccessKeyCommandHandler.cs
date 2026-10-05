using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Keys;

public sealed class DeleteAccessKeyCommandHandler(IAccessKeyRepository keys) : ICommandHandler<DeleteAccessKeyCommand>
{
    public async Task<Result> Handle(DeleteAccessKeyCommand command, CancellationToken cancellationToken) =>
        await keys.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false) ? Result.Success() : Result.Failure(KeyErrors.NotFound(command.Id));
}
