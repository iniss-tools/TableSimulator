using System.Globalization;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Events;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Logging;

/// <summary>
/// Zaznamy servera cez Serilog: konzola a denne subory v priecinku <c>logs</c> nastaveni (14 dni).
/// </summary>
public static class SerilogSetup
{
    private const string ConsoleTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
    private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>Zaznam do konzoly, kym sa server nezostavi (chyby pri starte).</summary>
    public static Logger CreateBootstrapLogger() => new LoggerConfiguration()
        .WriteTo.Console(outputTemplate: ConsoleTemplate, formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();

    /// <summary>Zaznam servera.</summary>
    public static LoggerConfiguration Configure(LoggerConfiguration configuration, AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(paths);
        return configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: ConsoleTemplate, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(Path.Combine(paths.LogsFolder, "tablesimulator-.log"), outputTemplate: FileTemplate,
                formatProvider: CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
    }

    /// <summary>
    /// Poziadavky: uspesne len pri podrobnom zazname (prikazy zapisuju dekoratory), odmietnute volania API ako
    /// upozornenie (chybajuci subor mimo API, napr. favicon.ico, nie), chyby servera ako chyba.
    /// </summary>
    public static void ConfigureRequestLogging(RequestLoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.GetLevel = (context, _, exception) =>
            exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
            : context.Response.StatusCode >= 400 && context.Request.Path.StartsWithSegments("/api") ? LogEventLevel.Warning
            : LogEventLevel.Debug;
    }
}
