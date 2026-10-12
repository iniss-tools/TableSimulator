using System.Globalization;
using FluentValidation;
using TableSimulator.Contracts.Boards;
using TableSimulator.Core.Boards;

namespace TableSimulator.Application.Boards;

/// <summary>Rozumne medze profilu tabule (tabula zo samoucenia moze mat az 256 riadkov).</summary>
public sealed class BoardProfileValidator : AbstractValidator<BoardProfileDto>
{
    public BoardProfileValidator()
    {
        RuleFor(p => p.Width).InclusiveBetween(8, 4096).WithErrorCode(BoardErrors.Width);
        RuleFor(p => p.Rows).InclusiveBetween(1, 256).WithErrorCode(BoardErrors.Rows);
        RuleFor(p => p.RowHeight).InclusiveBetween(5, 64).WithErrorCode(BoardErrors.RowHeight);
        RuleFor(p => p.AlternationMs).GreaterThanOrEqualTo(100).WithErrorCode(BoardErrors.Alternation);
        RuleFor(p => p.BlinkOnMs).GreaterThanOrEqualTo(50).WithErrorCode(BoardErrors.Blink);
        RuleFor(p => p.BlinkOffMs).GreaterThanOrEqualTo(50).WithErrorCode(BoardErrors.Blink);
        RuleFor(p => p.ScrollSpeed).InclusiveBetween(1, 500).WithErrorCode(BoardErrors.ScrollSpeed);
        RuleFor(p => p.Header!.Height).InclusiveBetween(1, 200).WithErrorCode(BoardErrors.HeaderHeight).When(p => p.Header is not null);
        RuleFor(p => p.RotationMs).InclusiveBetween(0, 60000).WithErrorCode(BoardErrors.Rotation);
        RuleFor(p => p.Flaps).Must(FlapProfileRules.IsValid).WithErrorCode(BoardErrors.Flaps)
            .WithMessage("Flap modules or lists are out of range.");
        RuleFor(p => p.CharFonts).Must(IsValidCharFonts).WithErrorCode(BoardErrors.CharFonts)
            .WithMessage("Character fonts must map font numbers 0-63 to a known dot font.");
    }

    /// <summary>Cisla pisma 0-63 a zname bodove pisma (prazdne = predvolene).</summary>
    private static bool IsValidCharFonts(IReadOnlyDictionary<string, CharFontDto>? fonts) =>
        fonts is null
        || (fonts.Count <= CharFont.MaxNumber + 1
            && fonts.All(f => int.TryParse(f.Key, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n <= CharFont.MaxNumber
                              && f.Value is not null && (string.IsNullOrEmpty(f.Value.Font) || CharFont.Fonts.Contains(f.Value.Font, StringComparer.Ordinal))));
}
