using FluentValidation;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Users;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(AuthErrors.UserName);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.UserName).Must(n => UserNameRules.IsValid(n?.Trim())).WithErrorCode(AuthErrors.UserName)
                .WithMessage("User name must have 2-64 characters: letters, digits, '.', '_' or '-'.");
            RuleFor(c => c.Request.DisplayName).MaximumLength(100).WithErrorCode(AuthErrors.DisplayName);
            RuleFor(c => c.Request.Role).Must(r => Roles.All.Contains(r)).WithErrorCode(UserErrors.Role);
            RuleFor(c => c.Request.Password).NotNull().MinimumLength(UserNameRules.MinPasswordLength).WithErrorCode(AuthErrors.Password);
        });
    }
}
