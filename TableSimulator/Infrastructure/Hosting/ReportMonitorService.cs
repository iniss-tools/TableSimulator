using Microsoft.Extensions.Logging;
using TableSimulator.Application.Archive;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Monitor hlaseni: kazde dve sekundy (ako XML tabul) pozrie najnovsi denny archiv hlaseni v priecinkoch logov INISSu
/// a pribudnute zaznamy posle prehliadacom.
/// </summary>
public sealed class ReportMonitorService(ReportMonitor monitor, ISettingsStore settings, TimeProvider time, ILogger<ReportMonitorService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(XmlTableWatcherService.Interval, time);
        string? lastError = null;
        do
        {
            try
            {
                monitor.Poll(settings.Current.VirtualFolders);
                lastError = null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // subor moze byt chvilu zamknuty - do logu len zmena chyby
                if (e.Message != lastError)
                    ServerLog.ReportMonitorFailed(logger, e.Message);
                lastError = e.Message;
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
