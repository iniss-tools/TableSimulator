using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.FileProviders;
using TableSimulator;
using TableSimulator.Api;
using TableSimulator.Core.Monitor;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

// parametre: --data <priecinok> (nastavenia), --config <subor> (linky a tabule), --port <cislo>, --no-browser
if (Arg("--data") is { } dataFolder)
    AppSettings.UseDataFolder(dataFolder);
var configPath = Arg("--config") ?? ConfigStore.DefaultPath;
var settings = AppSettings.Load();
if (int.TryParse(Arg("--port"), NumberStyles.None, CultureInfo.InvariantCulture, out var port))
    settings = settings with { HttpPort = port };

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory, WebRootPath = webRoot });
builder.WebHost.UseUrls(string.Create(CultureInfo.InvariantCulture, $"http://{(settings.AllowNetwork ? "0.0.0.0" : "localhost")}:{settings.HttpPort}"));
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

var simulator = new Simulator();
using var store = new ConfigStore(simulator, configPath);
store.Load();
await using var transport = new TransportManager(simulator);
await transport.SyncAsync();
using var hub = new LiveHub(simulator, () => settings.ConnectHost);
// pisma: vlastny priecinok, potom pisma programu ELEN Monitor
var fonts = new FontCatalog(() => [settings.FontsFolder, Monitor().FontFolder, Monitor().SimFolder]);
var designs = new DesignCatalog(Path.Combine(webRoot, "designs"), () => settings.EffectiveDesignsFolder);
using var designWatcher = WatchDesigns(settings.EffectiveDesignsFolder, hub);
await using var recordings = new RecordingService(simulator, transport, hub);

simulator.Changed += e =>
{
    if (e is StructureChanged or BoardChanged)
        store.ScheduleSave();
};

app.UseWebSockets();
app.UseDefaultFiles();
// vlastne vzhlady maju prednost pred vstavanymi s rovnakym id
if (Directory.Exists(settings.EffectiveDesignsFolder))
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(settings.EffectiveDesignsFolder), RequestPath = "/designs" });
app.UseStaticFiles();

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
        return Results.BadRequest();
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.ServeAsync(socket, context.Request.Query["log"] == "1", context.RequestAborted);
    return Results.Empty;
});

var api = app.MapGroup("/api");
api.MapGet("/state", () => simulator.Read(s => new StateDto(
    s.Lines.Select(l => Dto.From(l, settings.ConnectHost)).ToArray(),
    s.Boards.Select(Dto.From).ToArray(),
    Simulator.SupportedClasses.ToArray(),
    Dto.Manufacturers,
    Settings())));

api.MapGet("/boards/{id}", (string id) => simulator.Read(s => s.FindBoard(id) is { } b ? Results.Ok(Dto.From(b)) : Results.NotFound()));

api.MapPut("/lines/{number:int}", async (int number, LineConfig config) =>
{
    try
    {
        simulator.SetLine(config with { Number = number });
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }

    await transport.SyncAsync();
    store.ScheduleSave();
    return Results.NoContent();
});

api.MapDelete("/lines/{number:int}", async (int number) =>
{
    if (!simulator.RemoveLine(number))
        return Results.NotFound();
    await transport.SyncAsync();
    return Results.NoContent();
});

api.MapPut("/boards/{line:int}/{address:int}", (int line, int address, BoardConfig config) =>
{
    try
    {
        simulator.SetBoard(config with { Line = line, Address = address });
        return Results.NoContent();
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

api.MapDelete("/boards/{id}", (string id) => simulator.RemoveBoard(id) ? Results.NoContent() : Results.NotFound());
api.MapPost("/boards/{id}/fault", (string id, TableSimulator.Core.Boards.BoardFault fault) =>
    simulator.SetFault(id, fault) ? Results.NoContent() : Results.NotFound());
api.MapPost("/boards/{id}/clear", (string id) => simulator.ClearBoard(id) ? Results.NoContent() : Results.NotFound());

api.MapGet("/log", (long? after) => simulator.LogSince(after ?? 0).Select(Dto.From).ToArray());
api.MapDelete("/log", () =>
{
    simulator.ClearLog();
    return Results.NoContent();
});

api.MapGet("/recordings", recordings.List);
api.MapPost("/recordings/start", (StartRecordingRequest request) => recordings.StartAsync(request.Note));
api.MapPost("/recordings/stop", recordings.StopAsync);
api.MapDelete("/recordings/{name}", (string name) => recordings.Delete(name) ? Results.NoContent() : Results.NotFound());
api.MapGet("/recordings/{name}/file", (string name) =>
    RecordingService.PathFor(name) is { } path && File.Exists(path)
        ? Results.File(path, "application/x-ndjson", name + TableSimulator.Core.Recording.RecordingFile.Extension)
        : Results.NotFound());
api.MapPost("/recordings/upload", async (HttpRequest request, string? name) =>
    await recordings.UploadAsync(name ?? "zaznam", request.Body) is { } info ? Results.Ok(info) : Results.BadRequest(new { error = "format" }));
api.MapPost("/replay", async (PlayRequest request) =>
    await recordings.PlayAsync(request.Name, request.Speed, request.Loop) is { } status ? Results.Ok(status) : Results.NotFound());
api.MapPost("/replay/stop", async () =>
{
    await recordings.StopReplayAsync();
    return Results.NoContent();
});

api.MapGet("/monitor", () =>
{
    var monitor = Monitor();
    return new MonitorDto(monitor.Exists, monitor.Root, monitor.Exists ? monitor.Panels().ToArray() : [], monitor.Projects().ToArray(), monitor.Look());
});
// linky a tabule stanice INISSu (GVDEditor pri presmerovani liniek na simulator)
api.MapPost("/station", async (StationImport import) =>
{
    var result = simulator.ImportStation(import);
    await transport.SyncAsync();
    return Results.Ok(result);
});

api.MapPost("/monitor/import", (MonitorImportRequest request) =>
{
    var monitor = Monitor();
    var entries = monitor.Project(request.Project);
    if (entries.Count == 0)
        return Results.NotFound();
    var manufacturer = simulator.Read(s => s.FindLine(request.Line)?.Manufacturer);
    if (manufacturer == null)
        return Results.BadRequest(new { error = "line" });

    var count = 0;
    foreach (var entry in entries)
    {
        if (monitor.Panel(entry.ConfigFile) is not { } template)
            continue;
        simulator.SetBoard(new BoardConfig(request.Line, entry.Addresses[0], manufacturer)
        {
            Name = string.IsNullOrWhiteSpace(entry.Location) ? template.Name : entry.Location,
            Profile = template.Profile,
            SecondAddress = entry.Addresses.Count > 1 ? entry.Addresses[1] : null
        });
        count++;
    }

    return Results.Ok(new { count });
});

api.MapGet("/fonts", fonts.List);
api.MapGet("/fonts/{file}", (string file) => fonts.Get(file) is { } f ? Results.Ok(f) : Results.NotFound());
api.MapGet("/designs", designs.List);

api.MapGet("/settings", Settings);
api.MapPut("/settings", (SettingsDto dto) =>
{
    var restart = dto.HttpPort != settings.HttpPort || dto.AllowNetwork != settings.AllowNetwork || dto.DesignsFolder != settings.DesignsFolder;
    settings = settings with
    {
        HttpPort = dto.HttpPort, AllowNetwork = dto.AllowNetwork, FontsFolder = dto.FontsFolder.Trim(), DesignsFolder = dto.DesignsFolder.Trim(),
        ConnectHost = dto.ConnectHost.Trim(),
        MonitorFolder = dto.MonitorFolder.Trim()
    };
    settings.Save();
    hub.Broadcast(new { type = "structure" });
    return Results.Ok(new { restart });
});

var url = string.Create(CultureInfo.InvariantCulture, $"http://localhost:{settings.HttpPort}/");
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"TableSimulator: {url}  (linky a tabule: {configPath})");
    if (!args.Contains("--no-browser"))
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
});

await app.RunAsync();
store.Save();
return;

SettingsDto Settings() => new(settings.HttpPort, settings.AllowNetwork, settings.FontsFolder, settings.DesignsFolder, settings.ConnectHost, configPath)
{
    MonitorFolder = settings.MonitorFolder
};

MonitorFolder Monitor() => new(settings.MonitorFolder);

string? Arg(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static FileSystemWatcher? WatchDesigns(string folder, LiveHub hub)
{
    if (!Directory.Exists(folder))
        return null;
    var watcher = new FileSystemWatcher(folder) { IncludeSubdirectories = true, EnableRaisingEvents = true };
    FileSystemEventHandler changed = (_, e) =>
    {
        var relative = Path.GetRelativePath(folder, e.FullPath);
        var id = relative.Split(Path.DirectorySeparatorChar)[0];
        hub.Broadcast(new { type = "design", id });
    };
    watcher.Changed += changed;
    watcher.Created += changed;
    watcher.Deleted += changed;
    return watcher;
}

/// <summary>Zaciatok zaznamu.</summary>
internal sealed record StartRecordingRequest(string? Note);

/// <summary>Prehratie zaznamu.</summary>
internal sealed record PlayRequest(string Name, double Speed, bool Loop);

/// <summary>Import projektu ELEN Monitora na linku.</summary>
internal sealed record MonitorImportRequest(string Project, int Line);

/// <summary>Instalacia ELEN Monitora pre webove rozhranie.</summary>
internal sealed record MonitorDto(bool Available, string Folder, BoardTemplate[] Panels, string[] Projects, MonitorLook Look);
