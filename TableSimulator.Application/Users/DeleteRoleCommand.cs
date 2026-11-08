using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Users;

/// <summary>Odstrani vlastnu rolu.</summary>
public sealed record DeleteRoleCommand(string Name) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Name;
}

public sealed class DeleteRoleCommandHandler(IRoleService service, RoleCatalog catalog, ICurrentUser user) : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        if (catalog.Find(command.Name) is not { } role)
            return Result.Failure(RoleErrors.NotFound(command.Name));
        if (Roles.IsBuiltIn(role.Name))
            return Result.Failure(RoleErrors.BuiltIn());
        if (!catalog.Covers(user.Role, role.Permissions))
            return Result.Failure(RoleErrors.TooHigh());
        return await service.DeleteAsync(role.Name, cancellationToken).ConfigureAwait(false);
    }
}
