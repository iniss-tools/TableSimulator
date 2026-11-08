using FluentValidation;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Roles;

namespace TableSimulator.Application.Users;

/// <summary>Zalozi vlastnu rolu.</summary>
public sealed record CreateRoleCommand(SetRoleRequest Request) : ICommand<RoleCreatedDto>, IAuditable
{
    string IAuditable.AuditTarget => Request.DisplayName;
}

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator() => RuleFor(c => c.Request).NotNull().WithErrorCode(RoleErrors.Name).SetValidator(new SetRoleRules());
}

public sealed class CreateRoleCommandHandler(IRoleService service, RoleCatalog catalog, ICurrentUser user) : ICommandHandler<CreateRoleCommand, RoleCreatedDto>
{
    public async Task<Result<RoleCreatedDto>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        if (!catalog.Covers(user.Role, command.Request.Permissions))
            return Result.Failure<RoleCreatedDto>(RoleErrors.TooHigh());
        var created = await service.CreateAsync(command.Request.DisplayName.Trim(), command.Request.Permissions, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess ? Result.Success(new RoleCreatedDto(created.Value)) : Result.Failure<RoleCreatedDto>(created.Error);
    }
}
