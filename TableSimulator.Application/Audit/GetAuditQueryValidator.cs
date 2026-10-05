using FluentValidation;

namespace TableSimulator.Application.Audit;

public sealed class GetAuditQueryValidator : AbstractValidator<GetAuditQuery>
{
    public GetAuditQueryValidator() => RuleFor(q => q.Limit).InclusiveBetween(1, 500).WithErrorCode("audit.limit");
}
