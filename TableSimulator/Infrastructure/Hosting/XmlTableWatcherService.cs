using Microsoft.Extensions.Logging;
using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Live;
using TableSimulator.Core.Virtual;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Kazde dve sekundy prejde priecinky logov INISSu z nastavenia a zmeny XML tabul posle prehliadacom (virtualne
/// tabule si obsah nacitaju znova). Prechadza sa namiesto sledovania udalosti suboroveho systemu - funguje aj na
/// sietovom priecinku a INISS subor aj tak prepisuje najviac raz za par sekund.
/// </summary>
public sealed class XmlTableWatcherService(XmlTableFolders folders, ISettingsStore settings, ILiveNotifier live, TimeProvider time,
    ILogger<XmlTableWatcherService> logger) : BackgroundService
{
    /// <summary>Ako casto sa priecinky prechadzaju (INISSView 2 rovnako).</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        do
        {
            try
            {
                Poll();
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                ServerLog.XmlTablesFailed(logger, e);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private void Poll()
    {
        var before = folders.Sources.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        var changed = folders.Poll(settings.Current.VirtualFolders, time.GetLocalNow().DateTime);
        if (changed.Count == 0)
            return;

        foreach (var name in changed)
        {
            var was = before.GetValueOrDefault(name);
            switch (folders.Find(name))
            {
                case null:
                    ServerLog.XmlTableRemoved(logger, name);
                    break;
                case { Error: { } error } when was?.Error != error:
                    ServerLog.XmlTableUnreadable(logger, name, error);
                    break;
                case { Error: null, Table: { } table } source when was is null || was.Error is not null:
                    ServerLog.XmlTableFound(logger, name, source.Path, table.Station, table.Key);
                    break;
            }
        }

        live.Publish(new VirtualMessage(changed, false));
    }
}
