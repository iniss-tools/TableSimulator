using Microsoft.Extensions.Logging;

namespace TableSimulator.Infrastructure.Persistence;

internal static partial class DatabaseLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Heslo k PostgreSQL v {File} je teraz zasifrovane pre tento pocitac")]
    public static partial void PasswordProtected(ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Databaza: {Database}")]
    public static partial void Using(ILogger logger, string database);

    [LoggerMessage(Level = LogLevel.Information, Message = "Nova databaza PostgreSQL prevzala nastavenie servera zo SQLite {File}")]
    public static partial void SettingsFromSqlite(ILogger logger, string file);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vybrana databaza: {Database} - pouzije sa po restarte servera")]
    public static partial void Selected(ILogger logger, string database);
}
