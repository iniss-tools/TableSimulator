using System.Text.RegularExpressions;
using FluentValidation;

namespace TableSimulator.Application.Pwa;

public sealed partial class GetWebManifestQueryValidator : AbstractValidator<GetWebManifestQuery>
{
    public const string ViewCode = "pwa.view";
    public const string NameCode = "pwa.name";
    public const string KeyCode = "pwa.key";

    public GetWebManifestQueryValidator()
    {
        RuleFor(q => q.View).Must(v => v is not null && ViewPattern().IsMatch(v)).WithErrorCode(ViewCode)
            .WithMessage("The view must be a kiosk page (/view/<line-address>, /wall or /virtual/<number>).");
        RuleFor(q => q.Name).MaximumLength(80).WithErrorCode(NameCode);
        RuleFor(q => q.Key).Must(k => k is null || KeyPattern().IsMatch(k)).WithErrorCode(KeyCode)
            .WithMessage("The access key has an invalid format.");
    }

    [GeneratedRegex(@"^/(view/\d{1,3}-\d{1,3}|wall|virtual/\d{1,6})$", RegexOptions.CultureInvariant)]
    private static partial Regex ViewPattern();

    [GeneratedRegex("^ts_[A-Za-z0-9_-]{20,100}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
