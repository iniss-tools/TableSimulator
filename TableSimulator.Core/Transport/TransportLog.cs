using Microsoft.Extensions.Logging;
using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Transport;

/// <summary>
/// Zaznamy prenosovej vrstvy - spojenia liniek s INISSom a zrkadla na seriove porty.
/// </summary>
internal static partial class TransportLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Linka {Line} caka na INISS ({Endpoint})")]
    public static partial void Listening(ILogger logger, int line, LineEndpoint endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Linka {Line}: INISS pripojeny ({Remote})")]
    public static partial void Connected(ILogger logger, int line, string? remote);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Linka {Line}: INISS odpojeny")]
    public static partial void Disconnected(ILogger logger, int line);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Linka {Line} ({Endpoint}) nebezi: {Error}")]
    public static partial void Failed(ILogger logger, int line, LineEndpoint endpoint, string? error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Linka {Line}: predstierany vypadok - spojenie zavrete")]
    public static partial void Outage(ILogger logger, int line);

    [LoggerMessage(Level = LogLevel.Information, Message = "Linka {Line} zastavena")]
    public static partial void Stopped(ILogger logger, int line);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Zrkadlo linky {Line} ({Port}) nezapisuje: {Error}")]
    public static partial void MirrorFailed(ILogger logger, int line, LineEndpoint port, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Zrkadlo linky {Line} ({Port}) zapisuje")]
    public static partial void MirrorWriting(ILogger logger, int line, LineEndpoint port);
}
