using System.Diagnostics;
using Serilog;
using Serilog.Extensions.Logging;
using TableSimulator.Api;
using TableSimulator.Application;
using TableSimulator.Infrastructure;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.Backup;
using TableSimulator.Infrastructure.Hosting;
using TableSimulator.Infrastructure.Https;
using TableSimulator.Infrastructure.Logging;
using TableSimulator.Infrastructure.Mcp;
using TableSimulator.Infrastructure.OAuth;
using TableSimulator.Infrastructure.OpenApi;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Infrastructure.Realtime;
using TableSimulator.Infrastructure.Settings;
using TableSimulator.Application.Settings;
using TableSimulator.Persistence;

// parametre: --data <priecinok> (nastavenia a databaza), --port <cislo>, --no-browser, --reset-admin [meno] (nove heslo
// spravcu a koniec), --install-service / --uninstall-service (sluzba Windows a koniec)
Log.Logger = SerilogSetup.CreateBootstrapLogger();
try
{
    var commandLine = CommandLine.Parse(args, AppContext.BaseDirectory, WindowsService.IsRunning);
    if (commandLine.InstallService)
        return WindowsService.Install(commandLine, Console.Out);
    if (commandLine.UninstallService)
        return WindowsService.Uninstall(Console.Out);
    var paths = commandLine.Paths;
    // Nad jednym priecinkom nastaveni bezi jeden server - dalsie spustenie (odkaz v ponuke Start, ked bezi sluzba) len
    // otvori bieziaci. Zamok sa berie skor, nez sa siahne na databazu (obnova zalohy, migracie).
    if (!commandLine.ResetAdmin && RunningServer.BelongsToService(paths.DataFolder))
        return await RunningServer.OpenAsync(paths.DataFolder, commandLine.NoBrowser, service: true);
    using var serverLock = commandLine.ResetAdmin ? null : ServerLock.TryAcquire(paths.DataFolder);
    if (!commandLine.ResetAdmin && serverLock is null)
        return await RunningServer.OpenAsync(paths.DataFolder, commandLine.NoBrowser, service: false);
    if (commandLine.ObsoleteConfig)
        Log.Warning("Parameter --config je zruseny - linky a tabule su v databaze; subor nahrajte na stranke Linky (Importovat)");
    // nastavenie je v databaze - treba ho skor, nez sa server zostavi (porty, pristup zo siete, prihlasovanie)
    using var bootstrapLog = new SerilogLoggerFactory(Log.Logger);
    // databaza podla database.json v priecinku nastaveni (bez neho SQLite)
    var database = DatabaseConfig.ToConnection(paths, DatabaseConfig.Load(paths, bootstrapLog.CreateLogger(typeof(DatabaseConfig).FullName!)));
    // obnova zalohy pripravena z webu - skor, nez sa databaza otvori
    await ServerBackup.ApplyPendingAsync(paths, database, bootstrapLog.CreateLogger(typeof(ServerBackup).FullName!), DateTimeOffset.Now);
    var settings = new DbSettingsStore(database,
        await DatabaseBootstrap.PrepareAsync(paths, database, bootstrapLog.CreateLogger(typeof(DatabaseBootstrap).FullName!)), commandLine.Port);
    var endpoints = ServerEndpoints.From(settings.Current);

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory, WebRootPath = paths.WebRoot });
    builder.WebHost.ConfigureKestrel(endpoints.Configure);
    // mimo sluzby Windows bez ucinku
    builder.Host.UseWindowsService(options => options.ServiceName = WindowsService.Name);
    builder.Services.AddSerilog((_, logger) => SerilogSetup.Configure(logger, paths));
    builder.Services.AddProblemDetails();
    builder.Services.AddInfrastructure(paths, settings, endpoints);
    builder.Services.AddPersistence(database);
    builder.Services.AddSingleton(new DatabaseInfo(database.Provider == DatabaseProvider.Postgres ? DatabaseConfig.Postgres : DatabaseConfig.Sqlite,
        database.Description));
    builder.Services.AddAuth(paths, new AuthMode(settings.Current.LoginRequired));
    builder.Services.AddApplication();
    builder.Services.AddEndpoints(typeof(Program).Assembly);
    builder.Services.AddApiDocument();
    builder.Services.AddMcp();

    var app = builder.Build();
    await DatabaseInitializer.InitializeAsync(app.Services);
    if (commandLine.ResetAdmin)
        return await AdminReset.RunAsync(app.Services, commandLine.ResetAdminName, Console.Out);
    if (endpoints.HttpsEnabled)
        app.Services.GetRequiredService<HttpsCertificates>().Initialize();

    app.UseExceptionHandler();
    app.UseHttpsForNetwork();
    app.UseWebSockets();
    app.UseDefaultFiles();
    app.UseDesignFiles();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging(SerilogSetup.ConfigureRequestLogging);
    app.UseOAuthCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCsrfHeader();
    app.UseUserLogContext();
    app.MapLiveSocket("/ws");
    app.MapMcpServer();
    app.MapOAuth();
    app.MapApiDocument();
    app.MapEndpoints(app.MapGroup("/api"));
    app.MapInissViewFiles();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("TableSimulator: {Url} {HttpsUrl}(nastavenia: {DataFolder}, prihlasovanie: {Login})", endpoints.LocalUrl,
            endpoints.LocalHttpsUrl is { } https ? https + " " : "", paths.DataFolder,
            settings.Current.LoginRequired ? "zapnute" : "vypnute - len tento pocitac");
        serverLock?.Publish(endpoints.LocalUrl);
        if (!commandLine.NoBrowser)
            Process.Start(new ProcessStartInfo(endpoints.LocalUrl) { UseShellExecute = true })?.Dispose();
    });
    // zamok hned po zastaveni servera, nie az na konci procesu - restart sluzby aj testy spustaju dalsi server hned
    app.Lifetime.ApplicationStopped.Register(() => serverLock?.Dispose());

    await app.RunAsync();
    return 0;
}
catch (Exception e) when (e is not HostAbortedException)
{
    Log.Fatal(e, "TableSimulator sa nepodarilo spustit");
    ConsoleWindow.PauseIfOwned(Console.Out);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Vstupny bod servera (aj pre testy cez WebApplicationFactory).</summary>
public partial class Program;
