using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Faces;

namespace TableSimulator.Persistence.Faces;

/// <summary>Vlastne predlohy ciel tabul v databaze.</summary>
public sealed class FaceRepository(AppDbContext db) : IFaceRepository
{
    public async Task<IReadOnlyList<FaceRecord>> ListAsync(CancellationToken cancellationToken) =>
        await db.Faces.AsNoTracking().OrderBy(f => f.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<FaceRecord?> FindAsync(string id, CancellationToken cancellationToken) =>
        db.Faces.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task SaveAsync(FaceRecord face, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(face);
        if (await db.Faces.AnyAsync(f => f.Id == face.Id, cancellationToken).ConfigureAwait(false))
            db.Faces.Update(face);
        else
            db.Faces.Add(face);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken) =>
        await db.Faces.Where(f => f.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;
}
