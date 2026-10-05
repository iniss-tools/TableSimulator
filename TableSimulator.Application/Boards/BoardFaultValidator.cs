using FluentValidation;
using TableSimulator.Contracts.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Predstierana porucha: oneskorenie najviac minuta, stavove bity jeden bajt.</summary>
public sealed class BoardFaultValidator : AbstractValidator<BoardFaultDto>
{
    public BoardFaultValidator()
    {
        RuleFor(f => f.Kind).IsInEnum().WithErrorCode(BoardErrors.FaultKind);
        RuleFor(f => f.DelayMs).InclusiveBetween(0, 60_000).WithErrorCode(BoardErrors.FaultDelay);
        RuleFor(f => f.Status).InclusiveBetween(0, 255).WithErrorCode(BoardErrors.FaultStatus);
    }
}
