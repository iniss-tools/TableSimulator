using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Archive;

/// <summary>Chyby a kody chyb archivu.</summary>
public static class ArchiveErrors
{
    public const string Page = "archive.page";

    /// <summary>Najviac zaznamov na stranke.</summary>
    public const int MaxLimit = 1000;

    public static Error NotFound(DateOnly date) =>
        Error.NotFound("archive.notFound", string.Create(CultureInfo.InvariantCulture, $"No archive for {date:yyyy-MM-dd}."));

    public static Error Unreadable(string message) => Error.Problem("archive.unreadable", $"The archive file cannot be read: {message}");
}
