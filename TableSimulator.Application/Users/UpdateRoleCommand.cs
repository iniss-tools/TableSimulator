using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Roles;

namespace TableSimulator.Application.Users;

/// <summary>Zmeni nazov a opravnenia roly (vstavanej len opravnenia, spravcu nie).</summary>
public sealed record UpdateRoleCommand(string Name, SetRoleRequest Request) : ICommand, IAuditable
{
    string IAuditable.AuditTarget => Name;
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator() =>
        RuleFor(c => c.Request).NotNull().WithErrorCode(RoleErrors.Name).SetValidator((command, _) => new SetRoleRules(!Roles.IsBuiltIn(command.Name)));
}

public sealed class UpdateRoleCommandHandler(IRoleService service, RoleCatalog catalog, ICurrentUser user) : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (catalog.Find(command.Name) is not { } role)
            return Result.Failure(RoleErrors.NotFound(command.Name));
        if (role.Name == Roles.Admin)
            return Result.Failure(RoleErrors.Admin());
        if (role.Name == user.Role)
            return Result.Failure(RoleErrors.Self());
        if (!catalog.Covers(user.Role, role.Permissions) || !catalog.Covers(user.Role, command.Request.Permissions))
            return Result.Failure(RoleErrors.TooHigh());
        return await service.UpdateAsync(role.Name, (command.Request.DisplayName ?? "").Trim(), command.Request.Permissions, cancellationToken).ConfigureAwait(false);
    }
}
