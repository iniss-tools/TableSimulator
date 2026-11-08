using Microsoft.Extensions.Logging;
using TableSimulator.Application.Devices;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Kazdu sekundu posunie zariadenia, ktore sa menia s casom aj bez spravy INISSu (ustredne AK Signal), a nastavi im okolie
/// podla nastavenia (<see cref="DeviceClock" />).
/// </summary>
public sealed class DeviceClockService(DeviceClock clock, TimeProvider time, ILogger<DeviceClockService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        string? lastError = null;
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    clock.Tick();
                    lastError = null;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // nastavenie moze ukazovat na nedostupny disk - do logu len zmena chyby
                    if (e.Message != lastError)
                        ServerLog.DeviceClockFailed(logger, e);
                    lastError = e.Message;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ukoncenie servera
        }
    }
}
