using Microsoft.EntityFrameworkCore;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Persistence.Simulation;

/// <summary>Linky a tabule simulatora v databaze - nacitaju sa pri starte, ulozia sa vzdy cele naraz.</summary>
public sealed class SimulatorConfigRepository(AppDbContext db)
{
    /// <summary>Linky a tabule v poradi cisla linky a adresy.</summary>
    public async Task<SimulatorConfig> LoadAsync(CancellationToken cancellationToken)
    {
        var lines = await db.Lines.AsNoTracking().OrderBy(l => l.Number).ToListAsync(cancellationToken).ConfigureAwait(false);
        var boards = await db.Boards.AsNoTracking().OrderBy(b => b.Line).ThenBy(b => b.Address).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new SimulatorConfig
        {
            Lines = lines.Select(l => new LineConfig(l.Number, l.TableClass, l.Endpoint)
            {
                Name = l.Name, Mode = l.Mode, AutoLearn = l.AutoLearn, Manufacturer = l.Manufacturer, Mirror = l.Mirror
            }).ToList(),
            Boards = boards.Select(b => new BoardConfig(b.Line, b.Address, b.Manufacturer)
            {
                Name = b.Name, Profile = b.Profile, Design = b.Design, Fault = new BoardFault(b.FaultKind, b.FaultDelayMs, b.FaultStatus),
                SecondAddress = b.SecondAddress
            }).ToList()
        };
    }

    /// <summary>Nahradi vsetky linky a tabule (v transakcii, ak volajuci nezacal vlastnu).</summary>
    public async Task SaveAsync(SimulatorConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        var own = db.Database.CurrentTransaction is null;
        await using var transaction = own ? await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false) : null;
        await db.Boards.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await db.Lines.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        var lines = config.Lines.ToDictionary(l => l.Number);
        db.Lines.AddRange(lines.Values.Select(l => new LineRecord
        {
            Number = l.Number, TableClass = l.TableClass, Endpoint = l.Endpoint, Name = l.Name, Mode = l.Mode, AutoLearn = l.AutoLearn,
            Manufacturer = l.Manufacturer, Mirror = l.Mirror
        }));
        // tabula bez linky by neprisla cez cudzi kluc (simulator take nema)
        db.Boards.AddRange(config.Boards.Where(b => lines.ContainsKey(b.Line)).DistinctBy(b => (b.Line, b.Address)).Select(b => new BoardRecord
        {
            Line = b.Line, Address = b.Address, Manufacturer = b.Manufacturer, Name = b.Name, Profile = b.Profile, Design = b.Design,
            FaultKind = b.Fault.Kind, FaultDelayMs = b.Fault.DelayMs, FaultStatus = b.Fault.Status, SecondAddress = b.SecondAddress
        }));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        db.ChangeTracker.Clear();
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
