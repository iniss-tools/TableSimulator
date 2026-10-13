using System.Text.RegularExpressions;
using FluentValidation;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Application.Designs;

public sealed partial class SetDesignCommandValidator : AbstractValidator<SetDesignCommand>
{
    /// <summary>Kreslenie bodov (LED aj LCD).</summary>
    public const string Dots = "led-matrix";

    /// <summary>Kreslenie listovych tabul.</summary>
    public const string Flaps = "flaps";

    /// <summary>Farby listov, ktore material listovej tabule musi mat.</summary>
    private static readonly string[] FlapColors = ["black", "red", "yellow", "white"];

    public SetDesignCommandValidator()
    {
        RuleFor(c => c.Id).Must(id => id is not null && IdPattern().IsMatch(id)).WithErrorCode(DesignErrors.Id)
            .WithMessage("The design id must be 1-40 lowercase letters, digits and hyphens.");
        RuleFor(c => c.Request).NotNull().WithErrorCode(DesignErrors.Name);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Name).NotEmpty().MaximumLength(80).WithErrorCode(DesignErrors.Name);
            RuleFor(c => c.Request.Renderer).Must(r => r is Dots or Flaps).WithErrorCode(DesignErrors.Renderer);
            RuleFor(c => c.Request.Pitch).InclusiveBetween(1, 50).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Dot).InclusiveBetween(0.1, 1).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Glow).InclusiveBetween(0, 1).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.RowGap).InclusiveBetween(0, 50).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Padding).InclusiveBetween(0, 50).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.ModuleMargin).InclusiveBetween(0, 20).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Shape).IsInEnum().WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Frame).NotNull().WithErrorCode(DesignErrors.Number);
            When(c => c.Request.Frame is not null, () =>
            {
                RuleFor(c => c.Request.Frame.Width).InclusiveBetween(0, 100).WithErrorCode(DesignErrors.Number);
                RuleFor(c => c.Request.Frame.Radius).InclusiveBetween(0, 100).WithErrorCode(DesignErrors.Number);
                RuleFor(c => c.Request.Frame.Style).IsInEnum().WithErrorCode(DesignErrors.Number);
                RuleFor(c => c.Request.Frame.Badge).NotNull().MaximumLength(20).WithErrorCode(DesignErrors.Name);
            });
            RuleFor(c => c.Request.Seam).NotNull().WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Seam.Width).InclusiveBetween(0, 2).WithErrorCode(DesignErrors.Number).When(c => c.Request.Seam is not null);
            RuleFor(c => c.Request.Header).NotNull().WithErrorCode(DesignErrors.Font);
            When(c => c.Request.Header is not null, () =>
            {
                RuleFor(c => c.Request.Header.Font).NotEmpty().MaximumLength(200).WithErrorCode(DesignErrors.Font);
                RuleFor(c => c.Request.Header.ClockFont).NotEmpty().MaximumLength(200).WithErrorCode(DesignErrors.Font);
                RuleFor(c => c.Request.Header.Styles).Must(HasValidStyles).WithErrorCode(DesignErrors.Font)
                    .WithMessage("Text style names must be letters and digits, fonts at most 200 characters.");
            });
            RuleFor(c => c.Request.Flaps).NotNull().WithErrorCode(DesignErrors.Renderer).When(c => c.Request.Renderer == Flaps);
            RuleFor(c => c.Request.Flaps!.Font).NotEmpty().MaximumLength(200).WithErrorCode(DesignErrors.Font).When(c => c.Request.Flaps is not null);
            RuleFor(c => c.Request.Flaps!.Colors).Must(colors => colors is not null && FlapColors.All(colors.ContainsKey)).WithErrorCode(DesignErrors.Color)
                .WithMessage("Flap colors must contain black, red, yellow and white.").When(c => c.Request.Flaps is not null);
            RuleFor(c => Colors(c)).Must(colors => colors.All(IsColor)).WithName("Colors").WithErrorCode(DesignErrors.Color)
                .WithMessage("Colors must be #rrggbb.");
        });
    }

    // vsetky farby vzhladu; volitelne (null = nekreslit, pozadie, ako pasy) sa nahradia platnou
    private static IEnumerable<string?> Colors(SetDesignCommand c)
    {
        var r = c.Request;
        IEnumerable<string?> colors =
        [
            r.Background, r.Frame?.Color, r.Header?.Background, r.Header?.Color, r.Header?.ClockColor, r.Header?.ClockBackground,
            .. (r.Colors ?? new Dictionary<string, string>()).Values,
            r.Off ?? "#000000", r.GapColor ?? "#000000", r.Plate ?? "#000000", r.Seam?.Color ?? "#000000", r.Header?.Panel ?? "#000000",
            .. (r.Header?.Styles ?? new Dictionary<string, DesignTextStyleDto>()).Values.Select(s => s?.Color ?? "#000000")
        ];
        if (r.Flaps is { } flaps)
            colors = colors.Concat([flaps.Row, flaps.Split, flaps.Unknown])
                .Concat((flaps.Colors ?? new Dictionary<string, DesignFlapColorDto>()).Values.SelectMany(f => new[] { f?.Top, f?.Bottom, f?.Text }));
        return colors;
    }

    private static bool HasValidStyles(IReadOnlyDictionary<string, DesignTextStyleDto>? styles) =>
        styles is null || styles.All(s => StyleNamePattern().IsMatch(s.Key) && s.Value is not null && (s.Value.Font?.Length ?? 0) <= 200);

    private static bool IsColor(string? value) => value is not null && ColorPattern().IsMatch(value);

    /// <summary>Platny kluc vzhladu (meno priecinka).</summary>
    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();

    [GeneratedRegex("^[a-z][a-zA-Z0-9]{0,29}$", RegexOptions.CultureInvariant)]
    private static partial Regex StyleNamePattern();
}
