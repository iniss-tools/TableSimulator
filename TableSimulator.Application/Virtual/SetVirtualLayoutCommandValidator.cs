using System.Text.RegularExpressions;
using FluentValidation;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Application.Virtual;

public sealed partial class SetVirtualLayoutCommandValidator : AbstractValidator<SetVirtualLayoutCommand>
{
    public SetVirtualLayoutCommandValidator()
    {
        RuleFor(c => c.Id).Must(id => id is not null && IdPattern().IsMatch(id)).WithErrorCode(VirtualErrors.LayoutId)
            .WithMessage("The layout key may contain lower case letters, digits and dashes (at most 40).");
        RuleFor(c => c.Layout).NotNull().WithErrorCode(VirtualErrors.LayoutName);
        When(c => c.Layout is not null, () =>
        {
            RuleFor(c => c.Layout.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(VirtualErrors.LayoutName);
            RuleFor(c => c.Layout.Title).NotNull().MaximumLength(200).WithErrorCode(VirtualErrors.LayoutTitle);
            RuleFor(c => c.Layout.Rows).InclusiveBetween(1, 40).WithErrorCode(VirtualErrors.LayoutRows);
            RuleFor(c => c.Layout.Font).Must(f => f is null || f.Length <= 100 && f.IndexOfAny([';', '{', '}', '<', '>']) < 0)
                .WithErrorCode(VirtualErrors.LayoutFont);
            RuleFor(c => c.Layout.Colors).NotNull().Must(HasValidColors).WithErrorCode(VirtualErrors.LayoutColor)
                .WithMessage("Colors must be #rrggbb.");
            RuleFor(c => c.Layout.Columns).NotNull().Must(c => c.Count <= VirtualErrors.MaxColumns).WithErrorCode(VirtualErrors.LayoutColumn);
            RuleForEach(c => c.Layout.Columns).Must(IsValidColumn).WithErrorCode(VirtualErrors.LayoutColumn)
                .WithMessage("Column is not valid.");
            RuleFor(c => c.Layout.AlternationMs).InclusiveBetween(300, 20000).WithErrorCode(VirtualErrors.LayoutTiming);
            RuleFor(c => c.Layout.BlinkMs).InclusiveBetween(100, 5000).WithErrorCode(VirtualErrors.LayoutTiming);
            RuleFor(c => c.Layout.BlinkPhases).InclusiveBetween(2, 10).WithErrorCode(VirtualErrors.LayoutTiming);
        });
    }

    private static bool HasValidColors(VirtualColorsDto? colors) =>
        colors is not null
        && new[] { colors.Background, colors.Text, colors.HeadBackground, colors.HeadText, colors.Accent, colors.Muted, colors.Delay, colors.RowLine }
            .All(c => c is not null && ColorPattern().IsMatch(c))
        && (string.IsNullOrEmpty(colors.RowAlternate) || ColorPattern().IsMatch(colors.RowAlternate));

    private static bool IsValidColumn(VirtualColumnDto? column) =>
        column is { Cells.Count: >= 1 and <= 4 }
        && column.Cells.All(c => c is not null && CellPattern().IsMatch(c))
        && (column.Label?.Length ?? 0) <= 50
        && WidthPattern().IsMatch(column.Width ?? "")
        && ColumnColorPattern().IsMatch(column.Color ?? "")
        && column.Size is >= 30 and <= 300
        && (column.Separator?.Length ?? 0) <= 10;

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();

    [GeneratedRegex("^(|accent|muted|delay|#[0-9a-fA-F]{6})$", RegexOptions.CultureInvariant)]
    private static partial Regex ColumnColorPattern();

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9]{0,29}$", RegexOptions.CultureInvariant)]
    private static partial Regex CellPattern();

    [GeneratedRegex("^(|fill|[0-9]{1,3}(%|em))$", RegexOptions.CultureInvariant)]
    private static partial Regex WidthPattern();
}
