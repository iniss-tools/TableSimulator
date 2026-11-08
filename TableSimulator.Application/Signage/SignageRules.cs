using System.Text.RegularExpressions;

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
