using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Virtual;

namespace TableSimulator.Persistence.Virtual;

/// <summary>Definicie virtualnych tabul v databaze.</summary>
public sealed class VirtualBoardRepository(AppDbContext db) : IVirtualBoardRepository
{
    public async Task<IReadOnlyList<VirtualBoard>> ListAsync(CancellationToken cancellationToken) =>
        await db.VirtualBoards.AsNoTracking().OrderBy(b => b.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<VirtualBoard?> FindAsync(int id, CancellationToken cancellationToken) =>
        db.VirtualBoards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<bool> SaveAsync(VirtualBoard board, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(board);
        var exists = await db.VirtualBoards.AnyAsync(b => b.Id == board.Id, cancellationToken).ConfigureAwait(false);
        if (exists)
            db.VirtualBoards.Update(board);
        else
            db.VirtualBoards.Add(board);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
        return !exists;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        await db.VirtualBoards.Where(b => b.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;

    public async Task<VirtualImportCounts> ImportAsync(IReadOnlyList<VirtualBoard> boards, bool replace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(boards);
        var existing = (await db.VirtualBoards.Select(b => b.Id).ToListAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();
        var counts = new VirtualImportCounts();
        foreach (var board in boards)
        {
            if (!existing.Contains(board.Id))
            {
                db.VirtualBoards.Add(board);
                counts = counts with { Added = counts.Added + 1 };
            }
            else if (replace)
            {
                db.VirtualBoards.Update(board);
                counts = counts with { Replaced = counts.Replaced + 1 };
            }
            else
            {
                counts = counts with { Skipped = counts.Skipped + 1 };
            }
        }

        // SaveChanges zapise vsetko v jednej transakcii
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
        return counts;
    }
}
