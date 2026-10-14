using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.OAuth;

public sealed class DeleteOAuthGrantCommandHandler(IOAuthRepository repository, ICurrentUser user, RoleCatalog roles) : ICommandHandler<DeleteOAuthGrantCommand>
{
    public async Task<Result> Handle(DeleteOAuthGrantCommand command, CancellationToken cancellationToken)
    {
        var grant = (await repository.ListGrantsAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(g => g.Id == command.Id);
        if (grant is null)
            return Result.Failure(OAuthErrors.GrantNotFound(command.Id));
        // vlastne povolenie zrusi kazdy; cudzie spravca klucov, ak jeho rola pokryva rolu povolenia
        var own = grant.UserName == user.UserName && !user.IsAccessKey;
        if (!own && !(roles.Has(user.Role, Permissions.KeysManage) && roles.Covers(user.Role, roles.PermissionsOf(grant.Role))))
            return Result.Failure(OAuthErrors.GrantForbidden());
        return await repository.DeleteGrantAsync(command.Id, cancellationToken).ConfigureAwait(false)
            ? Result.Success()
            : Result.Failure(OAuthErrors.GrantNotFound(command.Id));
    }
}
