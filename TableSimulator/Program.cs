using System.Diagnostics;
using Serilog;
using Serilog.Extensions.Logging;
using TableSimulator.Api;
using TableSimulator.Application;
using TableSimulator.Infrastructure;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.Hosting;
using TableSimulator.Infrastructure.Https;
using TableSimulator.Infrastructure.Logging;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Infrastructure.Realtime;
using TableSimulator.Infrastructure.Settings;
using TableSimulator.Persistence;

// parametre: --data <priecinok> (nastavenia a databaza), --port <cislo>, --no-browser, --reset-admin [meno] (nove heslo
// spravcu a koniec)
Log.Logger = SerilogSetup.CreateBootstrapLogger();
try
{
    var commandLine = CommandLine.Parse(args, AppContext.BaseDirectory);
    var paths = commandLine.Paths;
    if (commandLine.ObsoleteConfig)
        Log.Warning("Parameter --config je zruseny - linky a tabule su v databaze; subor nahrajte na stranke Linky (Importovat)");
    // nastavenie je v databaze - treba ho skor, nez sa server zostavi (porty, pristup zo siete, prihlasovanie)
    using var bootstrapLog = new SerilogLoggerFactory(Log.Logger);
    var settings = new DbSettingsStore(paths.DatabaseFile,
        await DatabaseBootstrap.PrepareAsync(paths, bootstrapLog.CreateLogger(typeof(DatabaseBootstrap).FullName!)), commandLine.Port);
    var endpoints = ServerEndpoints.From(settings.Current);

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory, WebRootPath = paths.WebRoot });
    builder.WebHost.ConfigureKestrel(endpoints.Configure);
    builder.Services.AddSerilog((_, logger) => SerilogSetup.Configure(logger, paths));
    builder.Services.AddProblemDetails();
    builder.Services.AddInfrastructure(paths, settings, endpoints);
    builder.Services.AddPersistence(paths.DatabaseFile);
    builder.Services.AddAuth(paths, new AuthMode(settings.Current.LoginRequired));
    builder.Services.AddApplication();
    builder.Services.AddEndpoints(typeof(Program).Assembly);

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
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCsrfHeader();
    app.UseUserLogContext();
    app.MapLiveSocket("/ws");
    app.MapEndpoints(app.MapGroup("/api"));

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("TableSimulator: {Url} {HttpsUrl}(nastavenia: {DataFolder}, prihlasovanie: {Login})", endpoints.LocalUrl,
            endpoints.LocalHttpsUrl is { } https ? https + " " : "", paths.DataFolder,
            settings.Current.LoginRequired ? "zapnute" : "vypnute - len tento pocitac");
        if (!commandLine.NoBrowser)
            Process.Start(new ProcessStartInfo(endpoints.LocalUrl) { UseShellExecute = true })?.Dispose();
    });

    await app.RunAsync();
    return 0;
}
catch (Exception e) when (e is not HostAbortedException)
{
    Log.Fatal(e, "TableSimulator sa nepodarilo spustit");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Vstupny bod servera (aj pre testy cez WebApplicationFactory).</summary>
public partial class Program;
