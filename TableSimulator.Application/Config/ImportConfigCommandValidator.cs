using FluentValidation;

namespace TableSimulator.Application.Config;

public sealed class ImportConfigCommandValidator : AbstractValidator<ImportConfigCommand>
{
    /// <summary>Najvacsi subor (znaky).</summary>
    public const int MaxLength = 5_000_000;

    public ImportConfigCommandValidator() =>
        RuleFor(c => c.Content).NotEmpty().WithErrorCode(ConfigErrors.Content).WithMessage("The file is empty.")
            .MaximumLength(MaxLength).WithErrorCode(ConfigErrors.Content).WithMessage("The file is too large.");
}
