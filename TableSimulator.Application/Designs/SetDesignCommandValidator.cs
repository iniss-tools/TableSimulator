using System.Text.RegularExpressions;
using FluentValidation;

namespace TableSimulator.Application.Designs;

public sealed partial class SetDesignCommandValidator : AbstractValidator<SetDesignCommand>
{
    public SetDesignCommandValidator()
    {
        RuleFor(c => c.Id).Must(id => id is not null && IdPattern().IsMatch(id)).WithErrorCode(DesignErrors.Id)
            .WithMessage("The design id must be 1-40 lowercase letters, digits and hyphens.");
        RuleFor(c => c.Request).NotNull().WithErrorCode(DesignErrors.Name);
        When(c => c.Request is not null, () =>
        {
            RuleFor(c => c.Request.Name).NotEmpty().MaximumLength(80).WithErrorCode(DesignErrors.Name);
            RuleFor(c => c.Request.Renderer).Equal("led-matrix").WithErrorCode(DesignErrors.Renderer);
            RuleFor(c => c.Request.Pitch).InclusiveBetween(1, 50).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Dot).InclusiveBetween(0.1, 1).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Glow).InclusiveBetween(0, 1).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.RowGap).InclusiveBetween(0, 50).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Padding).InclusiveBetween(0, 50).WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Shape).IsInEnum().WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Frame).NotNull().WithErrorCode(DesignErrors.Number);
            RuleFor(c => c.Request.Frame.Width).InclusiveBetween(0, 100).WithErrorCode(DesignErrors.Number).When(c => c.Request.Frame is not null);
            RuleFor(c => c.Request.Frame.Radius).InclusiveBetween(0, 100).WithErrorCode(DesignErrors.Number).When(c => c.Request.Frame is not null);
            RuleFor(c => c.Request.Header).NotNull().WithErrorCode(DesignErrors.Font);
            RuleFor(c => c.Request.Header.Font).NotEmpty().MaximumLength(200).WithErrorCode(DesignErrors.Font).When(c => c.Request.Header is not null);
            RuleFor(c => c.Request.Header.ClockFont).NotEmpty().MaximumLength(200).WithErrorCode(DesignErrors.Font).When(c => c.Request.Header is not null);
            RuleFor(c => Colors(c)).Must(colors => colors.All(IsColor)).WithName("Colors").WithErrorCode(DesignErrors.Color)
                .WithMessage("Colors must be #rrggbb.");
        });
    }

    // vsetky farby vzhladu; Off a GapColor smu chybat (null = nekreslit, pozadie)
    private static IEnumerable<string?> Colors(SetDesignCommand c) =>
    [
        c.Request.Background, c.Request.Frame?.Color, c.Request.Header?.Background, c.Request.Header?.Color,
        c.Request.Header?.ClockColor, c.Request.Header?.ClockBackground, .. (c.Request.Colors ?? new Dictionary<string, string>()).Values,
        c.Request.Off ?? "#000000", c.Request.GapColor ?? "#000000"
    ];

    private static bool IsColor(string? value) => value is not null && ColorPattern().IsMatch(value);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();
}
