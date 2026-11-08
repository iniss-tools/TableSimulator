using Microsoft.Extensions.Logging;

namespace TableSimulator.Infrastructure.Backup;

internal static partial class BackupLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Obnovena zaloha z {Created} (server {Version}); povodne udaje su v {Saved}")]
    public static partial void Restored(ILogger logger, DateTimeOffset created, string version, string saved);

    [LoggerMessage(Level = LogLevel.Error, Message = "Obnova zalohy zlyhala - server bezi s povodnymi udajmi, zaloha je v {Failed}")]
    public static partial void RestoreFailed(ILogger logger, Exception error, string failed);
}
