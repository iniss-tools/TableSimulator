using FluentValidation;

namespace TableSimulator.Application.Boards;

public sealed class SetBoardCommandValidator : AbstractValidator<SetBoardCommand>
{
    public SetBoardCommandValidator()
    {
        RuleFor(c => c.Line).InclusiveBetween(1, 100).WithErrorCode(BoardErrors.Line);
        RuleFor(c => c.Address).InclusiveBetween(0, 255).WithErrorCode(BoardErrors.Address);
        RuleFor(c => c.Request).NotNull().WithErrorCode(BoardErrors.Manufacturer);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Manufacturer).NotEmpty().WithErrorCode(BoardErrors.Manufacturer);
            RuleFor(c => c.Request.SecondAddress)
                .Must((c, second) => second is null || (second is > 0 and < 128 && second != c.Address))
                .WithErrorCode(BoardErrors.SecondAddress).WithMessage("Second address must be 1-127 and differ from the address.");
            RuleFor(c => c.Request.Profile).NotNull().WithErrorCode(BoardErrors.Width).SetValidator(new BoardProfileValidator());
            RuleFor(c => c.Request.Fault).NotNull().WithErrorCode(BoardErrors.FaultKind).SetValidator(new BoardFaultValidator());
        });
    }
}
