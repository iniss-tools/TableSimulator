using FluentValidation;

namespace TableSimulator.Application.Frames;

public sealed class GetLogQueryValidator : AbstractValidator<GetLogQuery>
{
    public GetLogQueryValidator() => RuleFor(q => q.After).GreaterThanOrEqualTo(0).WithErrorCode("log.after");
}
