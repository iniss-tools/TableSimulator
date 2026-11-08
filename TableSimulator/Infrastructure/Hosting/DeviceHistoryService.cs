using Microsoft.Extensions.Logging;
using TableSimulator.Application.Devices;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Historia ustredni AK Signal: kazdych par sekund odoberie zo simulatora zmeny stavu svetiel a skoncene hlasenia a zapise
/// ich do databazy, pri ukonceni servera aj zvysok. Pri starte a potom raz za den odstrani historiu starsiu nez
/// <see cref="DeviceHistoryRetention.Days" /> dni.
/// </summary>
public sealed class DeviceHistoryService(Simulator simulator, IServiceScopeFactory scopes, TimeProvider time, ILogger<DeviceHistoryService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private DateTimeOffset _nextCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        try
        {
            do
            {
                await CleanupAsync(stoppingToken).ConfigureAwait(false);
                await SaveAsync(stoppingToken).ConfigureAwait(false);
            } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ukoncenie servera
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var sample = simulator.TakeDeviceHistory();
        if (sample.IsEmpty)
            return;
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IDeviceHistoryRepository>().AddAsync(sample, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ServerLog.DeviceHistoryFailed(logger, e);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        if (now < _nextCleanup)
            return;
        _nextCleanup = now.AddDays(1);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var deleted = await scope.ServiceProvider.GetRequiredService<IDeviceHistoryRepository>()
                .DeleteOlderThanAsync(now.AddDays(-DeviceHistoryRetention.Days).ToUnixTimeMilliseconds(), cancellationToken).ConfigureAwait(false);
            if (deleted > 0)
                ServerLog.DeviceHistoryCleaned(logger, deleted, DeviceHistoryRetention.Days);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ServerLog.DeviceHistoryFailed(logger, e);
        }
    }
}
