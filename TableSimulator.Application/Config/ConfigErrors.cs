using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Config;

/// <summary>Chyby a kody chyb exportu a importu liniek a tabul.</summary>
public static class ConfigErrors
{
    public const string Content = "config.content";

    public static Error Invalid(string message) => Error.Problem("config.invalid", $"The file is not a valid simulator configuration: {message}");
}
