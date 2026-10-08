using Microsoft.Extensions.Logging;
using TableSimulator.Application.Designs;
using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Live;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Sleduje priecinok vlastnych vzhladov - zmena <c>design.json</c> sa na tabuliach prejavi hned.
/// </summary>
public sealed class DesignWatcherService(AppPaths paths, ISettingsStore settings, ILiveNotifier live, ILogger<DesignWatcherService> logger)
    : IHostedService, IDisposable
{
    private FileSystemWatcher? _watcher;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (DesignFolder.Prepare(paths, settings.Current) is not { } folder)
            return Task.CompletedTask;

        _watcher = new FileSystemWatcher(folder) { IncludeSubdirectories = true, EnableRaisingEvents = true };
        _watcher.Changed += (_, e) => Changed(folder, e.FullPath);
        _watcher.Created += (_, e) => Changed(folder, e.FullPath);
        _watcher.Deleted += (_, e) => Changed(folder, e.FullPath);
        ServerLog.WatchingDesigns(logger, folder);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void Changed(string folder, string path)
    {
        var id = Path.GetRelativePath(folder, path).Split(Path.DirectorySeparatorChar)[0];
        ServerLog.DesignChanged(logger, id);
        live.Publish(new DesignMessage(id));
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
