using FluentValidation;

namespace TableSimulator.Application.Auth;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.UserName).NotEmpty().WithErrorCode(AuthErrors.UserName);
        RuleFor(c => c.Password).NotEmpty().WithErrorCode(AuthErrors.Password);
    }
}
