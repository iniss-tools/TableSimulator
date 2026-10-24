using Microsoft.Extensions.Logging;
using TableSimulator.Application.Audit;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Pri starte a potom raz za den odstrani zaznamy auditu starsie nez <see cref="AuditEntry.RetentionDays" /> dni.</summary>
public sealed class AuditRetentionService(IServiceScopeFactory scopes, TimeProvider time, ILogger<AuditRetentionService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var log = scope.ServiceProvider.GetRequiredService<IAuditLog>();
                    var deleted = await log.DeleteOlderThanAsync(time.GetUtcNow().UtcDateTime.AddDays(-AuditEntry.RetentionDays), stoppingToken)
                        .ConfigureAwait(false);
                    if (deleted > 0)
                        ServerLog.AuditCleaned(logger, deleted, AuditEntry.RetentionDays);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    ServerLog.AuditCleanupFailed(logger, e);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ukoncenie servera - aj po neuspesnom starte, ked host sluzbu nezastavi a len ju pri uvolneni zrusi
        }
    }
}
