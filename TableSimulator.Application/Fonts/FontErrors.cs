using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Fonts;

/// <summary>Chyby a kody chyb pisiem.</summary>
public static class FontErrors
{
    public const string File = "font.file";

    public static Error NotFound(string file) => Error.NotFound("font.notFound", $"Font file '{file}' does not exist or is not an ELEN font.");
}
