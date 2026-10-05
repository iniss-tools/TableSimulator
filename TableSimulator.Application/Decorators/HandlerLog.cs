using Microsoft.Extensions.Logging;

namespace TableSimulator.Application.Decorators;

/// <summary>Zaznamy vybavenia prikazov a dotazov.</summary>
internal static partial class HandlerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Prikaz {Command} vybaveny za {ElapsedMs} ms")]
    public static partial void CommandCompleted(ILogger logger, string command, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dotaz {Query} vybaveny za {ElapsedMs} ms")]
    public static partial void QueryCompleted(ILogger logger, string query, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Request} skoncil chybou {ErrorCode}: {ErrorDescription}")]
    public static partial void Failed(ILogger logger, string request, string errorCode, string errorDescription);
}
