using System.Diagnostics;
using System.Globalization;
using Serilog;
using TableSimulator.Api;
using TableSimulator.Application;
using TableSimulator.Infrastructure;
using TableSimulator.Infrastructure.Hosting;
using TableSimulator.Infrastructure.Logging;
using TableSimulator.Infrastructure.Realtime;
using TableSimulator.Infrastructure.Settings;

// parametre: --data <priecinok> (nastavenia), --config <subor> (linky a tabule), --port <cislo>, --no-browser
Log.Logger = SerilogSetup.CreateBootstrapLogger();
try
{
    var commandLine = CommandLine.Parse(args, AppContext.BaseDirectory);
    var paths = commandLine.Paths;
    var settings = new JsonSettingsStore(paths, commandLine.Port);

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory, WebRootPath = paths.WebRoot });
    builder.WebHost.UseUrls(string.Create(CultureInfo.InvariantCulture,
        $"http://{(settings.Current.AllowNetwork ? "0.0.0.0" : "localhost")}:{settings.Current.HttpPort}"));
    builder.Services.AddSerilog((_, logger) => SerilogSetup.Configure(logger, paths));
    builder.Services.AddProblemDetails();
    builder.Services.AddInfrastructure(paths, settings);
    builder.Services.AddApplication();
    builder.Services.AddEndpoints(typeof(Program).Assembly);

    var app = builder.Build();
    app.UseExceptionHandler();
    app.UseWebSockets();
    app.UseDefaultFiles();
    app.UseDesignFiles();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging(SerilogSetup.ConfigureRequestLogging);
    app.MapLiveSocket("/ws");
    app.MapEndpoints(app.MapGroup("/api"));

    var url = string.Create(CultureInfo.InvariantCulture, $"http://localhost:{settings.Current.HttpPort}/");
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("TableSimulator: {Url} (nastavenia: {DataFolder})", url, paths.DataFolder);
        if (!commandLine.NoBrowser)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
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
