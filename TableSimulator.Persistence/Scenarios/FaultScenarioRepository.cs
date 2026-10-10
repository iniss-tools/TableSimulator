using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Scenarios;

namespace TableSimulator.Persistence.Scenarios;

/// <summary>Scenare porucha v databaze.</summary>
public sealed class FaultScenarioRepository(AppDbContext db) : IFaultScenarioRepository
{
    public async Task<IReadOnlyList<FaultScenarioRecord>> ListAsync(CancellationToken cancellationToken) =>
        await db.FaultScenarios.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<FaultScenarioRecord?> FindAsync(int id, CancellationToken cancellationToken) =>
        db.FaultScenarios.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<int> AddAsync(string name, string definition, CancellationToken cancellationToken)
    {
        var record = new FaultScenarioRecord(0, name, definition);
        db.FaultScenarios.Add(record);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var id = record.Id;
        db.ChangeTracker.Clear();
        return id;
    }

    public async Task<bool> UpdateAsync(FaultScenarioRecord scenario, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return await db.FaultScenarios.Where(s => s.Id == scenario.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.Name, scenario.Name).SetProperty(s => s.Definition, scenario.Definition), cancellationToken)
            .ConfigureAwait(false) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        await db.FaultScenarios.Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) > 0;
}
