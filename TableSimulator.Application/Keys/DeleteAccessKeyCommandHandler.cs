using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Keys;

public sealed class DeleteAccessKeyCommandHandler(IAccessKeyRepository keys, ICurrentUser user, RoleCatalog roles) : ICommandHandler<DeleteAccessKeyCommand>
{
    public async Task<Result> Handle(DeleteAccessKeyCommand command, CancellationToken cancellationToken)
    {
        var key = (await keys.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(k => k.Id == command.Id);
        if (key is null)
            return Result.Failure(KeyErrors.NotFound(command.Id));
        // kluc s vyssimi opravneniami odstrani len ten, kto ich ma
        if (!roles.Covers(user.Role, roles.PermissionsOf(key.Role)))
            return Result.Failure(KeyErrors.RoleTooHigh());
        return await keys.DeleteAsync(command.Id, cancellationToken).ConfigureAwait(false) ? Result.Success() : Result.Failure(KeyErrors.NotFound(command.Id));
    }
}
