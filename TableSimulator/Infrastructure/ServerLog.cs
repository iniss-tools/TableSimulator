using Microsoft.Extensions.Logging;

namespace TableSimulator.Infrastructure;

/// <summary>Zaznamy servera (spustenie, vzhlady, XML tabul).</summary>
internal static partial class ServerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "TableSimulator bezi na {Url}")]
    public static partial void Started(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vlastne vzhlady: {Folder}")]
    public static partial void WatchingDesigns(ILogger logger, string folder);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Zmeneny vzhlad {Design}")]
    public static partial void DesignChanged(ILogger logger, string design);

    [LoggerMessage(Level = LogLevel.Information, Message = "XML tabule {Name}: {Path} (stanica {Station}, tabula {Key})")]
    public static partial void XmlTableFound(ILogger logger, string name, string path, string station, string key);

    [LoggerMessage(Level = LogLevel.Warning, Message = "XML tabule {Name} sa neda precitat: {Error}")]
    public static partial void XmlTableUnreadable(ILogger logger, string name, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "XML tabule {Name} uz v priecinkoch nie je")]
    public static partial void XmlTableRemoved(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Audit: odstranenych {Count} zaznamov starsich nez {Days} dni")]
    public static partial void AuditCleaned(ILogger logger, int count, int days);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cistenie auditu zlyhalo")]
    public static partial void AuditCleanupFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Statistika: odstranenych {Count} zaznamov starsich nez {Days} dni")]
    public static partial void StatisticsCleaned(ILogger logger, int count, int days);

    [LoggerMessage(Level = LogLevel.Error, Message = "Zapis alebo cistenie statistiky komunikacie zlyhalo")]
    public static partial void StatisticsFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Prechod priecinkov s XML tabul zlyhal")]
    public static partial void XmlTablesFailed(ILogger logger, Exception exception);
}
