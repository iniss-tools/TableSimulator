using FluentValidation;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Playlist: platnost, okno a polozky.</summary>
public sealed class SignagePlaylistRules : AbstractValidator<SetSignagePlaylistRequest>
{
    public SignagePlaylistRules()
    {
        RuleFor(r => r.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(SignageErrors.Name);
        RuleFor(r => r.Period).IsInEnum().WithErrorCode(SignageErrors.Period);
        RuleFor(r => r.End).Must((r, end) => end is not { } e || e >= r.Start).When(r => r.Period != SignagePeriod.Once).WithErrorCode(SignageErrors.Dates)
            .WithMessage("The last day must not be before the first.");
        RuleFor(r => r.Weekdays).NotNull().WithErrorCode(SignageErrors.Weekdays);
        RuleForEach(r => r.Weekdays).InclusiveBetween(1, 7).WithErrorCode(SignageErrors.Weekdays);
        RuleFor(r => r.Weekdays).Must(d => d is { Count: > 0 }).When(r => r.Period == SignagePeriod.Weekly).WithErrorCode(SignageErrors.Weekdays)
            .WithMessage("Choose at least one day of the week.");
        RuleFor(r => r.From).InclusiveBetween(0, 24 * 60 - 1).WithErrorCode(SignageErrors.Window);
        RuleFor(r => r.To).InclusiveBetween(0, 24 * 60).WithErrorCode(SignageErrors.Window);
        RuleFor(r => r.Items).NotNull().Must(i => i is null || i.Count <= SignageErrors.MaxItems).WithErrorCode(SignageErrors.Items);
        RuleForEach(r => r.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.Message).GreaterThan(0).WithErrorCode(SignageErrors.Items);
            i.RuleFor(x => x.Duration).InclusiveBetween(1, SignageErrors.MaxDuration).When(x => x.Duration is not null).WithErrorCode(SignageErrors.Duration);
        });
    }
}
