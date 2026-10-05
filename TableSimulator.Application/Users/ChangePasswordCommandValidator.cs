using FluentValidation;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Users;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(AuthErrors.Password);
        RuleFor(c => c.Request.NewPassword).NotNull().MinimumLength(UserNameRules.MinPasswordLength).WithErrorCode(AuthErrors.Password)
            .When(c => c.Request is not null);
    }
}
