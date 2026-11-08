using FluentValidation;
using TableSimulator.Application.Virtual;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

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
