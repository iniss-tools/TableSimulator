using System.Text.RegularExpressions;
using FluentValidation;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Spolocne pravidla vstupov informacnych panelov.</summary>
internal static partial class SignageRules
{
    public static bool IsColor(string? color) => color is not null && ColorPattern().IsMatch(color);

    public static bool IsBoardId(string? id) => id is not null && BoardPattern().IsMatch(id);

    public static bool IsWebAddress(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && url!.Length <= 500;

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();

    [GeneratedRegex(@"^\d{1,3}-\d{1,3}$", RegexOptions.CultureInvariant)]
    private static partial Regex BoardPattern();
}

/// <summary>Obsah spravy podla druhu.</summary>
public sealed class SignageMessageRules : AbstractValidator<SetSignageMessageRequest>
{
    public SignageMessageRules()
    {
        RuleFor(r => r.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(SignageErrors.Name);
        RuleFor(r => r.Note).NotNull().MaximumLength(500).WithErrorCode(SignageErrors.Note);
        RuleFor(r => r.Kind).IsInEnum().WithErrorCode(SignageErrors.Kind);
        RuleFor(r => r.Duration).InclusiveBetween(1, SignageErrors.MaxDuration).WithErrorCode(SignageErrors.Duration);
        RuleFor(r => r.Background).Must(SignageRules.IsColor).WithErrorCode(SignageErrors.Color).WithMessage("The background must be a #rrggbb color.");
        RuleFor(r => r.Paragraphs).NotNull().WithErrorCode(SignageErrors.Paragraphs);
        RuleFor(r => r.VirtualBoard).NotNull().InclusiveBetween(1, VirtualErrors.MaxId).When(r => r.Kind == SignageMessageKind.Virtual)
            .WithErrorCode(SignageErrors.VirtualBoard);
        RuleFor(r => r.Board).Must(SignageRules.IsBoardId).When(r => r.Kind == SignageMessageKind.Board).WithErrorCode(SignageErrors.Board)
            .WithMessage("The board must be given as line-address.");
        RuleFor(r => r.Media).NotNull().When(r => r.Kind is SignageMessageKind.Image or SignageMessageKind.Video).WithErrorCode(SignageErrors.Media);
        RuleFor(r => r.Url).Must(SignageRules.IsWebAddress).When(r => r.Kind == SignageMessageKind.Web).WithErrorCode(SignageErrors.Url)
            .WithMessage("The address must be an absolute http or https URL.");
        When(r => r.Kind == SignageMessageKind.Text && r.Paragraphs is not null, () =>
        {
            RuleFor(r => r.Paragraphs.Count).InclusiveBetween(1, SignageErrors.MaxParagraphs).WithErrorCode(SignageErrors.Paragraphs);
            RuleForEach(r => r.Paragraphs).ChildRules(p =>
            {
                p.RuleFor(x => x.Text).Must(t => !string.IsNullOrWhiteSpace(t)).MaximumLength(2000).WithErrorCode(SignageErrors.Paragraphs);
                p.RuleFor(x => x.Color).Must(SignageRules.IsColor).WithErrorCode(SignageErrors.Color);
                p.RuleFor(x => x.Size).IsInEnum().WithErrorCode(SignageErrors.Paragraphs);
                p.RuleFor(x => x.Align).IsInEnum().WithErrorCode(SignageErrors.Paragraphs);
            });
        });
    }
}

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

/// <summary>Skupina: playlisty a predvoleny obsah.</summary>
public sealed class SignageGroupRules : AbstractValidator<SetSignageGroupRequest>
{
    public SignageGroupRules()
    {
        RuleFor(r => r.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(SignageErrors.Name);
        RuleFor(r => r.Note).NotNull().MaximumLength(500).WithErrorCode(SignageErrors.Note);
        RuleFor(r => r.Playlists).NotNull().Must(p => p is null || p.Count <= 100 && p.Distinct().Count() == p.Count).WithErrorCode(SignageErrors.Playlists);
        RuleFor(r => r.DefaultMessage).Null().When(r => r.DefaultPlaylist is not null).WithErrorCode(SignageErrors.Default)
            .WithMessage("A group has either a default playlist or a default message.");
    }
}

/// <summary>Panel.</summary>
public sealed class SignagePanelRules : AbstractValidator<SetSignagePanelRequest>
{
    public SignagePanelRules()
    {
        RuleFor(r => r.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(SignageErrors.Name);
        RuleFor(r => r.Note).NotNull().MaximumLength(500).WithErrorCode(SignageErrors.Note);
        RuleFor(r => r.Group).GreaterThan(0).When(r => r.Group is not null).WithErrorCode(SignageErrors.Group);
    }
}
