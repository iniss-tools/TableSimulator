using FluentValidation;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

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
