using Microsoft.Extensions.Logging;

namespace TableSimulator.Infrastructure.Alerts;

internal static partial class AlertLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Oznamenie {Event} odoslane ({Channel}): {Subject}")]
    public static partial void Sent(ILogger logger, string @event, string channel, string subject);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Oznamenie {Event} sa nepodarilo odoslat ({Channel}): {Error}")]
    public static partial void Failed(ILogger logger, string @event, string channel, string error);
}
