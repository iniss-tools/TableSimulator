using FluentValidation;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Roles;

namespace TableSimulator.Application.Users;

/// <summary>Nazov a opravnenia roly; <paramref name="requireName" /> = false pri vstavanej roli (nazov sa nemeni).</summary>
public sealed class SetRoleRules : AbstractValidator<SetRoleRequest>
{
    public SetRoleRules(bool requireName = true)
    {
        RuleFor(r => r.DisplayName).Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= Roles.MaxDisplayName).When(_ => requireName)
            .WithErrorCode(RoleErrors.Name).WithMessage($"The role name must have 1-{Roles.MaxDisplayName} characters.");
        RuleFor(r => r.Permissions).NotNull().WithErrorCode(RoleErrors.Permissions);
        When(r => r.Permissions is not null, () =>
        {
            RuleFor(r => r.Permissions).Must(p => p.All(Permissions.All.Contains)).WithErrorCode(RoleErrors.Permissions)
                .WithMessage("Unknown permission.");
            RuleFor(r => r.Permissions).Must(p => p.Contains(Permissions.SimulatorView)).WithErrorCode(RoleErrors.View)
                .WithMessage("Every role needs the overview permission.");
        });
    }
}
