using System.Buffers;
using FluentValidation;

namespace TableSimulator.Application.Virtual;

public sealed class SetVirtualBoardCommandValidator : AbstractValidator<SetVirtualBoardCommand>
{
    // znaky, ktore nemoze mat meno suboru vo Windows (SAVE_XML je meno suboru v priecinku logov)
    private static readonly SearchValues<char> InvalidFileChars = SearchValues.Create("\\/:*?\"<>|");

    public SetVirtualBoardCommandValidator()
    {
        RuleFor(c => c.Id).InclusiveBetween(1, VirtualErrors.MaxId).WithErrorCode(VirtualErrors.Id);
        RuleFor(c => c.Request).NotNull().WithErrorCode(VirtualErrors.Name);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(VirtualErrors.Name);
            RuleFor(c => c.Request.Description).MaximumLength(200).WithErrorCode(VirtualErrors.Description);
            RuleFor(c => c.Request.Source).Must(IsFileName).WithErrorCode(VirtualErrors.Source)
                .WithMessage("The XML file name must not be empty or contain a path.");
            RuleFor(c => c.Request.RowCount).InclusiveBetween(1, VirtualErrors.MaxRows).When(c => c.Request.RowCount is not null)
                .WithErrorCode(VirtualErrors.Rows);
            RuleFor(c => c.Request.RowStart).InclusiveBetween(1, VirtualErrors.MaxRows).WithErrorCode(VirtualErrors.Rows);
            RuleFor(c => c.Request.Separator).MaximumLength(20).WithErrorCode(VirtualErrors.Separator);
            RuleFor(c => c.Request.Template).MaximumLength(100).WithErrorCode(VirtualErrors.Template);
            RuleFor(c => c.Request.Parameter).MaximumLength(200).WithErrorCode(VirtualErrors.Parameter);
        });
    }

    private static bool IsFileName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 100 && name.AsSpan().IndexOfAny(InvalidFileChars) < 0 && !name.Any(char.IsControl);
}
