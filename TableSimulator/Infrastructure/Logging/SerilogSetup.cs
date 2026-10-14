using System.Globalization;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using TableSimulator.Application.Settings;
using TableSimulator.Infrastructure.Mcp;

namespace TableSimulator.Infrastructure.Logging;

/// <summary>
/// Zaznamy servera cez Serilog: konzola a denne subory v priecinku <c>logs</c> nastaveni (14 dni).
/// </summary>
public static class SerilogSetup
{
    private const string ConsoleTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
    private const string FileTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {User} {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>Vlastnost zaznamu s menom prihlaseneho pouzivatela (kto co urobil).</summary>
    public const string UserProperty = "User";

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
            // SDK MCP zapisuje kazde volanie - nastroje su dotazy, tie zapisuje dekorator len pri podrobnom zazname
            .MinimumLevel.Override("ModelContextProtocol", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty(UserProperty, "-")
            .WriteTo.Console(outputTemplate: ConsoleTemplate, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(Path.Combine(paths.LogsFolder, "tablesimulator-.log"), outputTemplate: FileTemplate,
                formatProvider: CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
    }

    /// <summary>Kazdy zaznam pocas poziadavky nesie meno pouzivatela (za prihlasenim, pred endpointmi).</summary>
    public static WebApplication UseUserLogContext(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.Use(async (context, next) =>
        {
            using (LogContext.PushProperty(UserProperty, context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "-"))
                await next(context);
        });
        return app;
    }

    /// <summary>
    /// Poziadavky: uspesne len pri podrobnom zazname (prikazy zapisuju dekoratory) okrem prihlasenia OAuth, odmietnute volania API, MCP a OAuth ako
    /// upozornenie (chybajuci subor mimo API, napr. favicon.ico, nie), chyby servera ako chyba. Zaznam je pred
    /// prihlasenim, aby zachytil aj poziadavky odmietnute bez prihlasenia alebo bez hlavicky CSRF.
    /// </summary>
    /// <summary>Server OAuth pre MCP - opisy servera, registracia, povolenie a tokeny.</summary>
    private static bool IsOAuth(PathString path) =>
        path.StartsWithSegments("/oauth") || path.StartsWithSegments("/.well-known/oauth-authorization-server")
        || path.StartsWithSegments("/.well-known/oauth-protected-resource");

    public static void ConfigureRequestLogging(RequestLoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.EnrichDiagnosticContext = (diagnostic, context) =>
            diagnostic.Set(UserProperty, context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "-");
        options.GetLevel = (context, _, exception) =>
            exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
            : context.Response.StatusCode >= 400 && (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments(McpSetup.Pattern)
                                                     || IsOAuth(context.Request.Path))
                ? LogEventLevel.Warning
            // prihlasenie klienta MCP je zriedkave a bez zaznamu sa neda zistit, kde sa zastavilo
            : IsOAuth(context.Request.Path) ? LogEventLevel.Information
            : LogEventLevel.Debug;
    }
}
