using FluentValidation;

namespace TableSimulator.Application.Fonts;

public sealed class GetFontQueryValidator : AbstractValidator<GetFontQuery>
{
    public GetFontQueryValidator() => RuleFor(q => q.File).Must(FontCatalog.IsValidFileName).WithErrorCode(FontErrors.File);
}
