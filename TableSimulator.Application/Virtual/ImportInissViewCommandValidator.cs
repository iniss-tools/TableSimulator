using FluentValidation;

namespace TableSimulator.Application.Virtual;

public sealed class ImportInissViewCommandValidator : AbstractValidator<ImportInissViewCommand>
{
    /// <summary>Najvacsi subor Web.config (znaky).</summary>
    public const int MaxLength = 1_000_000;

    public ImportInissViewCommandValidator() =>
        RuleFor(c => c.Content).NotEmpty().WithErrorCode(VirtualErrors.Content).MaximumLength(MaxLength).WithErrorCode(VirtualErrors.Content);
}
