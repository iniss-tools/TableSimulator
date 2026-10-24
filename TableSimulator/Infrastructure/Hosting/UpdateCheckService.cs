using TableSimulator.Application.Server;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Zistuje novu verziu servera minutu po starte a potom kazdych 12 hodin, ak to nastavenie dovoli.</summary>
public sealed class UpdateCheckService(IServerUpdates updates, ISettingsStore settings, TimeProvider time) : BackgroundService
{
    private static readonly TimeSpan FirstCheck = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!updates.Status.Release)
            return;
        using var timer = new PeriodicTimer(Interval, time);
        try
        {
            await Task.Delay(FirstCheck, time, stoppingToken).ConfigureAwait(false);
            do
            {
                if (settings.Current.CheckUpdates)
                    await updates.CheckAsync(stoppingToken).ConfigureAwait(false);
            } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ukoncenie servera - aj po neuspesnom starte, ked host sluzbu nezastavi a len ju pri uvolneni zrusi
        }
    }
}
