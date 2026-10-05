using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Virtual;

namespace TableSimulator.Persistence.Virtual;

/// <summary>Vlastne rozlozenia virtualnych tabul v databaze.</summary>
public sealed class VirtualLayoutRepository(AppDbContext db) : IVirtualLayoutRepository
{
    public async Task<IReadOnlyList<VirtualLayoutRecord>> ListAsync(CancellationToken cancellationToken) =>
        await db.VirtualLayouts.AsNoTracking().OrderBy(l => l.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<VirtualLayoutRecord?> FindAsync(string id, CancellationToken cancellationToken) =>
        db.VirtualLayouts.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task SaveAsync(VirtualLayoutRecord layout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (await db.VirtualLayouts.AnyAsync(l => l.Id == layout.Id, cancellationToken).ConfigureAwait(false))
            db.VirtualLayouts.Update(layout);
        else
            db.VirtualLayouts.Add(layout);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken) =>
        await db.VirtualLayouts.Where(l => l.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;
}
