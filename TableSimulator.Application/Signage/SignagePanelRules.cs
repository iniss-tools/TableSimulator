using FluentValidation;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Panel.</summary>
public sealed class SignagePanelRules : AbstractValidator<SetSignagePanelRequest>
{
    public SignagePanelRules()
    {
        RuleFor(r => r.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(SignageErrors.Name);
        RuleFor(r => r.Note).NotNull().MaximumLength(500).WithErrorCode(SignageErrors.Note);
        RuleFor(r => r.Group).GreaterThan(0).When(r => r.Group is not null).WithErrorCode(SignageErrors.Group);
    }
}
