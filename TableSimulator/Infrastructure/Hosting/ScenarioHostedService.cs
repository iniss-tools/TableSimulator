using Microsoft.Extensions.Logging;
using TableSimulator.Application.Scenarios;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Posuva scenare porucha v case (5x za sekundu). Pri ukonceni servera ich zastavi skor, nez sa ulozia linky a tabule -
/// poruchy zo scenara sa neulozia.
/// </summary>
public sealed class ScenarioHostedService(ScenarioRunner runner, ILogger<ScenarioHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Tick);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await runner.TickAsync().ConfigureAwait(false);
                }
                catch (Exception e) when (e is InvalidOperationException or IOException)
                {
                    ScenarioLog.TickFailed(logger, e);
                }
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
        await runner.StopAllAsync().ConfigureAwait(false);
    }
}

internal static partial class ScenarioLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Scenar porucha sa nepodarilo posunut")]
    public static partial void TickFailed(ILogger logger, Exception error);
}
