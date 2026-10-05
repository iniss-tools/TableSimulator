using FluentValidation;

namespace TableSimulator.Application.Auth;

public sealed class SetupCommandValidator : AbstractValidator<SetupCommand>
{
    public SetupCommandValidator()
    {
        RuleFor(c => c.UserName).Must(UserNameRules.IsValid).WithErrorCode(AuthErrors.UserName)
            .WithMessage("User name must have 2-64 characters: letters, digits, '.', '_' or '-'.");
        RuleFor(c => c.DisplayName).MaximumLength(100).WithErrorCode(AuthErrors.DisplayName);
        RuleFor(c => c.Password).MinimumLength(UserNameRules.MinPasswordLength).WithErrorCode(AuthErrors.Password);
    }
}
