using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Monitor;

/// <summary>Chyby a kody chyb importu z ELEN Monitora.</summary>
public static class MonitorErrors
{
    public const string Project = "monitor.project";
    public const string Line = "monitor.line";

    public static Error ProjectNotFound(string project) =>
        Error.NotFound("monitor.projectNotFound", $"ELEN Monitor project '{project}' does not exist or has no boards.");

    public static Error NotElenLine(int line) =>
        Error.Problem("monitor.notElenLine", string.Create(CultureInfo.InvariantCulture, $"Line {line} does not have ELEN boards."));
}
