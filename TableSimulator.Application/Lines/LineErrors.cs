using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Lines;

/// <summary>Chyby a kody chyb liniek.</summary>
public static class LineErrors
{
    public const string Number = "line.number";
    public const string TableClass = "line.tableClass";
    public const string Endpoint = "line.endpoint";
    public const string Mirror = "line.mirror";

    public static Error NotFound(int number) =>
        Error.NotFound("line.notFound", string.Create(CultureInfo.InvariantCulture, $"Line {number} does not exist."));

    public static Error Invalid(string message) => Error.Problem("line.invalid", message);
}
