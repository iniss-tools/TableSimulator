using FluentValidation;

namespace TableSimulator.Application.Archive;

public sealed class GetReportArchiveQueryValidator : AbstractValidator<GetReportArchiveQuery>
{
    public GetReportArchiveQueryValidator()
    {
        RuleFor(q => q.Offset).GreaterThanOrEqualTo(0).WithErrorCode(ArchiveErrors.Page);
        RuleFor(q => q.Limit).InclusiveBetween(1, ArchiveErrors.MaxLimit).WithErrorCode(ArchiveErrors.Page);
    }
}
