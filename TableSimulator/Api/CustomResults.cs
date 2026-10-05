using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Errors;

namespace TableSimulator.Api;

/// <summary>
/// Chyba vysledku ako odpoved ProblemDetails: stav HTTP podla druhu chyby, kod chyby vo vlastnosti <c>code</c>
/// a pri neplatnych vstupoch zoznam chyb vo vlastnosti <c>errors</c>.
/// </summary>
public static class CustomResults
{
    public static IResult Problem(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess)
            throw new InvalidOperationException("A success result is not a problem.");

        var error = result.Error;
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        if (error is ValidationError validation)
            extensions["errors"] = validation.Errors.Select(e => new ErrorDto(e.Code, e.Description)).ToArray();

        return Results.Problem(
            title: Title(error.Type),
            detail: error.Type == ErrorType.Failure ? "An unexpected error occurred." : error.Description,
            type: Type(error.Type),
            statusCode: Status(error.Type),
            extensions: extensions);
    }

    private static string Title(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Problem => "Request failed",
        ErrorType.NotFound => "Not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Server error"
    };

    private static string Type(ErrorType type) => type switch
    {
        ErrorType.Validation or ErrorType.Problem => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
        ErrorType.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };

    private static int Status(ErrorType type) => type switch
    {
        ErrorType.Validation or ErrorType.Problem => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
