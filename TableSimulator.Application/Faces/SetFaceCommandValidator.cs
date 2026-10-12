using System.Text.RegularExpressions;
using FluentValidation;
using TableSimulator.Contracts.Faces;

namespace TableSimulator.Application.Faces;

public sealed partial class SetFaceCommandValidator : AbstractValidator<SetFaceCommand>
{
    /// <summary>Najvacsia poloha a rozmer prvku v bodoch.</summary>
    private const double MaxDots = 4096;

    public SetFaceCommandValidator()
    {
        RuleFor(c => c.Id).Must(BoardFaces.IsValidId).WithErrorCode(FaceErrors.Id)
            .WithMessage("The face key may contain lower case letters, digits and dashes (at most 40).");
        RuleFor(c => c.Face).NotNull().WithErrorCode(FaceErrors.Name);
        When(c => c.Face is not null, () =>
        {
            RuleFor(c => c.Face.Name).Must(n => !string.IsNullOrWhiteSpace(n)).MaximumLength(100).WithErrorCode(FaceErrors.Name);
            RuleFor(c => c.Face.Margin).NotNull().Must(m => m is { Left: >= 0 and <= 1000, Top: >= 0 and <= 1000, Right: >= 0 and <= 1000, Bottom: >= 0 and <= 1000 })
                .WithErrorCode(FaceErrors.Margin).WithMessage("Margins must be 0-1000 dots.");
            RuleFor(c => c.Face.RowGap).InclusiveBetween(0, 200).WithErrorCode(FaceErrors.Rows).When(c => c.Face.RowGap is not null);
            RuleFor(c => c.Face.RowGroup).InclusiveBetween(0, 256).WithErrorCode(FaceErrors.Rows);
            RuleFor(c => c.Face.GroupGap).InclusiveBetween(0, 200).WithErrorCode(FaceErrors.Rows);
            RuleFor(c => c.Face.ModuleWidth).InclusiveBetween(0, 4096).WithErrorCode(FaceErrors.Margin);
            RuleFor(c => c.Face.Elements).NotNull().Must(e => e.Count <= FaceErrors.MaxElements).WithErrorCode(FaceErrors.Elements);
            RuleForEach(c => c.Face.Elements).Must(IsValidElement).WithErrorCode(FaceErrors.Element).WithMessage("Face element is not valid.");
            RuleForEach(c => c.Face.Elements).Must(e => e is null || IsValidImage(e.Image)).WithErrorCode(FaceErrors.Image)
                .WithMessage("An image must be a PNG, JPEG, GIF, WebP or SVG data URL of at most 400000 characters.");
        });
    }

    private static bool IsValidElement(FaceElementDto? e) =>
        e is not null
        && e.Row is >= 1 and <= 256
        && Within(e.X, MaxDots) && Within(e.Y, MaxDots) && Within(e.Width, MaxDots) && Within(e.Height, MaxDots)
        && (e.Text?.Length ?? 0) <= 500
        && e.Size is >= 1 and <= 400
        && e.Size2 is >= 0 and <= 400
        && ColorPattern().IsMatch(e.Color ?? "") && ColorPattern().IsMatch(e.Fill ?? "") && StylePattern().IsMatch(e.Style ?? "")
        && Enum.IsDefined(e.Kind) && Enum.IsDefined(e.Render) && Enum.IsDefined(e.Symbol)
        && e.Border is >= 0 and <= 50
        && e.Radius is >= 0 and <= 200
        && e.MaxWidth is >= 0 and <= MaxDots
        && e.Spacing is >= -10 and <= 50;

    /// <summary>Obrazok ako data URL beznych formatov webu, alebo ziadny.</summary>
    private static bool IsValidImage(string? image) =>
        string.IsNullOrEmpty(image) || (image.Length <= FaceErrors.MaxImage && ImagePattern().IsMatch(image));

    private static bool Within(double value, double limit) => double.IsFinite(value) && Math.Abs(value) <= limit;

    /// <summary>Farba <c>#rrggbb</c>, nazov farby vzhladu alebo nic.</summary>
    [GeneratedRegex("^(|#[0-9a-fA-F]{6}|[a-z][a-zA-Z0-9]{0,29})$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();

    /// <summary>Data URL obrazka: PNG, JPEG, GIF, WebP alebo SVG v base64.</summary>
    [GeneratedRegex(@"^data:image/(png|jpeg|gif|webp|svg\+xml);base64,[A-Za-z0-9+/]+={0,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex ImagePattern();

    /// <summary>Nazov stylu textu zo vzhladu alebo nic.</summary>
    [GeneratedRegex("^(|[a-z][a-zA-Z0-9]{0,29})$", RegexOptions.CultureInvariant)]
    private static partial Regex StylePattern();
}
