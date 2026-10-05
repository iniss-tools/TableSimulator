using FluentValidation;
using TableSimulator.Application.Auth;

namespace TableSimulator.Application.Keys;

public sealed class CreateAccessKeyCommandValidator : AbstractValidator<CreateAccessKeyCommand>
{
    public CreateAccessKeyCommandValidator()
    {
        RuleFor(c => c.Request).NotNull().WithErrorCode(KeyErrors.Name);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(KeyErrors.Name);
            RuleFor(c => c.Request.Kind).IsInEnum().WithErrorCode(KeyErrors.Kind);
            RuleFor(c => c.Request.Role).Must(r => Roles.All.Contains(r)).WithErrorCode(KeyErrors.Role);
        });
    }
}
