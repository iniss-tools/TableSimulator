using System.Globalization;
using ModelContextProtocol;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Mcp;

/// <summary>
/// Vysledok handlera v nastroji: chyba ide modelu ako chyba nastroja s kodom a popisom (ako <c>code</c> a <c>detail</c>
/// v ProblemDetails API), aby sa mohol opravit.
/// </summary>
public static class McpResults
{
    public static T ValueOrThrow<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? result.Value : throw new McpException(Describe(result.Error));
    }

    /// <summary>Prikaz bez hodnoty: chyba ide modelu ako chyba nastroja.</summary>
    public static void ThrowIfFailed(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
            throw new McpException(Describe(result.Error));
    }

    /// <summary>Limit poctu poloziek vysledku 1-<paramref name="max" />; inak chyba nastroja s kodom <paramref name="code" />.</summary>
    public static void CheckLimit(int limit, int max, string code)
    {
        if (limit < 1 || limit > max)
            throw new McpException(string.Create(CultureInfo.InvariantCulture, $"{code}: The limit must be 1-{max}."));
    }

    public static string Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error switch
        {
            ValidationError validation => string.Join("; ", validation.Errors.Select(e => $"{e.Code}: {e.Description}")),
            { Type: ErrorType.Failure } => $"{error.Code}: An unexpected error occurred.",
            _ => $"{error.Code}: {error.Description}"
        };
    }
}
