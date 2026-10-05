using FluentValidation;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Users;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(AuthErrors.DisplayName);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.DisplayName).MaximumLength(100).WithErrorCode(AuthErrors.DisplayName);
            RuleFor(c => c.Request.Role).Must(r => Roles.All.Contains(r)).WithErrorCode(UserErrors.Role);
        });
    }
}
