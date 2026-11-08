using Microsoft.Extensions.Logging;
using TableSimulator.Application.Alerts;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Infrastructure.Alerts;

/// <summary>
/// Kazde 2 s porovna stav liniek (aj ticho servera pri ustredniach AK Signal, <see cref="AlertLines" />) a oznami vypadky
/// a navraty kanalmi z nastavenia (<see cref="LineAlertTracker" />). Vypnute oznamenia na vypadky zabudnu.
/// </summary>
public sealed class LineAlertService(
    Simulator simulator,
    ISettingsStore settings,
    IEnumerable<IAlertChannel> channels,
    TimeProvider time,
    ILogger<LineAlertService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);
    private readonly LineAlertTracker _tracker = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                var alerts = settings.Current.Alerts;
                if (!alerts.Enabled)
                {
                    _tracker.Reset();
                    continue;
                }

                var (outages, recoveries) = _tracker.Update(AlertLines.Of(simulator), time.GetUtcNow(), alerts);
                if (outages.Count > 0)
                    await SendAsync(AlertKind.Outage, outages, alerts, stoppingToken).ConfigureAwait(false);
                if (recoveries.Count > 0)
                    await SendAsync(AlertKind.Recovery, recoveries, alerts, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ukoncenie servera - aj po neuspesnom starte, ked host sluzbu nezastavi a len ju pri uvolneni zrusi
        }
    }

    private async Task SendAsync(AlertKind kind, IReadOnlyList<AlertLine> lines, AlertSettings alerts, CancellationToken cancellationToken)
    {
        var message = AlertMessage.Create(kind, lines, time.GetUtcNow(), Environment.MachineName, alerts.Language, time.LocalTimeZone);
        foreach (var channel in channels.Where(c => c.IsConfigured(alerts)))
        {
            try
            {
                await channel.SendAsync(message, alerts, cancellationToken).ConfigureAwait(false);
                AlertLog.Sent(logger, message.Event, channel.Key, message.Subject);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                AlertLog.Failed(logger, message.Event, channel.Key, e.Message);
            }
        }
    }
}
