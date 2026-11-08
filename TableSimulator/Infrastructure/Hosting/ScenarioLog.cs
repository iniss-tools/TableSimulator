using Microsoft.Extensions.Logging;

namespace TableSimulator.Infrastructure.Hosting;

internal static partial class ScenarioLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Scenar porucha sa nepodarilo posunut")]
    public static partial void TickFailed(ILogger logger, Exception error);
}
