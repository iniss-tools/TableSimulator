using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Decorators;

/// <summary>Spolocny zapis vysledku pre dekoratory zaznamu.</summary>
internal static class LoggingDecorator
{
    public static void Log(ILogger logger, string name, Result result, long started, bool command)
    {
        if (result.IsFailure)
        {
            HandlerLog.Failed(logger, name, result.Error.Code, result.Error is ValidationError validation
                ? string.Join("; ", validation.Errors.Select(e => e.Code))
                : result.Error.Description);
            return;
        }

        var elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (command)
            HandlerLog.CommandCompleted(logger, name, elapsed);
        else
            HandlerLog.QueryCompleted(logger, name, elapsed);
    }
}
