using FluentValidation;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Users;

public sealed class SetUserPasswordCommandValidator : AbstractValidator<SetUserPasswordCommand>
{
    public SetUserPasswordCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(AuthErrors.Password);
        RuleFor(c => c.Request.Password).NotNull().MinimumLength(UserNameRules.MinPasswordLength).WithErrorCode(AuthErrors.Password)
            .When(c => c.Request is not null);
    }
}
