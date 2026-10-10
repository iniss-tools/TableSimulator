using Microsoft.Extensions.Logging;
using TableSimulator.Application.Statistics;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Statistics;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Statistika komunikacie: na zaciatku kazdej minuty odoberie zo simulatora pocitadla skoncenej minuty a zapise ich do
/// databazy, pri ukonceni servera aj rozpracovanu minutu. Pri starte a potom raz za den odstrani statistiku starsiu nez
/// <see cref="TrafficRetention.Days" /> dni.
/// </summary>
public sealed class StatisticsService(Simulator simulator, IServiceScopeFactory scopes, TimeProvider time, ILogger<StatisticsService> logger)
    : BackgroundService
{
    /// <summary>Odber kusok po zaciatku minuty - minuta je urcite skoncena.</summary>
    private static readonly TimeSpan AfterMinute = TimeSpan.FromMilliseconds(200);

    private DateTimeOffset _nextCleanup = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupAsync(stoppingToken).ConfigureAwait(false);
                var now = time.GetUtcNow();
                var next = TrafficMinutes.Start(TrafficMinutes.Of(now) + 1) + AfterMinute;
                await Task.Delay(next - now, time, stoppingToken).ConfigureAwait(false);
                // skoncena minuta je ta pred aktualnou
                await SaveAsync(TrafficMinutes.Of(time.GetUtcNow()) - 1, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // ukoncenie servera
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        // rozpracovana minuta - po restarte sa jej dalsie spravy pripocitaju
        await SaveAsync(TrafficMinutes.Of(time.GetUtcNow()), cancellationToken).ConfigureAwait(false);
    }

    private async Task SaveAsync(long minute, CancellationToken cancellationToken)
    {
        var sample = simulator.TakeTraffic(minute);
        if (sample.IsEmpty)
            return;
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ITrafficStatisticsRepository>().AddAsync(sample, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ServerLog.StatisticsFailed(logger, e);
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
            var deleted = await scope.ServiceProvider.GetRequiredService<ITrafficStatisticsRepository>()
                .DeleteOlderThanAsync(TrafficMinutes.Of(now.AddDays(-TrafficRetention.Days)), cancellationToken).ConfigureAwait(false);
            if (deleted > 0)
                ServerLog.StatisticsCleaned(logger, deleted, TrafficRetention.Days);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ServerLog.StatisticsFailed(logger, e);
        }
    }
}
