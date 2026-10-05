using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Audit;

namespace TableSimulator.Persistence.Audit;

/// <summary>Audit v databaze.</summary>
public sealed class AuditLog(AppDbContext db) : IAuditLog
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        // zaznam sa zapise samostatne - nie spolu s rozpracovanymi zmenami prikazu, ktory mohol zlyhat
        db.ChangeTracker.Clear();
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(long? before, string? user, string? action, int limit, CancellationToken cancellationToken)
    {
        var query = db.AuditEntries.AsNoTracking();
        if (before is { } id)
            query = query.Where(e => e.Id < id);
        if (user is not null)
            query = query.Where(e => e.User == user);
        if (action is not null)
            query = query.Where(e => e.Action == action);
        return await query.OrderByDescending(e => e.Id).Take(limit + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> DeleteOlderThanAsync(DateTime time, CancellationToken cancellationToken) =>
        db.AuditEntries.Where(e => e.Time < time).ExecuteDeleteAsync(cancellationToken);
}
