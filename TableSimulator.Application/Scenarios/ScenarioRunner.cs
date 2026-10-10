using TableSimulator.Application.Live;
using TableSimulator.Contracts.Live;
using TableSimulator.Contracts.Scenarios;
using TableSimulator.Core.Boards;
using TableSimulator.Core.Scenarios;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;

namespace TableSimulator.Application.Scenarios;

/// <summary>
/// Beziace scenare porucha. Pri kazdom tiku (server ho vola asi 5x za sekundu) zluci ucinky vsetkych beziacich scenarov
/// (porucha tabule z neskor spusteneho scenara prepise skorsi) a v simulatore zmeni len to, co sa od posledneho tiku
/// zmenilo. Tabula, ktoru uz ziadny scenar neovlada, dostane spat poruchu spred scenara; po vypadku napajania sa zapne
/// a linka po vypadku sa spusti znova. Ruca zmena poruchy pocas scenara plati do dalsej zmeny scenara.
/// </summary>
public sealed class ScenarioRunner(Simulator simulator, TransportManager transport, ILiveNotifier live, TimeProvider time) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lock _lock = new();
    private readonly Dictionary<int, Run> _runs = [];

    // co scenare v simulatore nastavili
    private readonly Dictionary<string, BoardFault> _applied = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BoardFault> _original = new(StringComparer.Ordinal);
    private readonly HashSet<string> _poweredOff = new(StringComparer.Ordinal);
    private readonly HashSet<int> _outages = [];
    private volatile bool _disposed;

    private sealed class Run(FaultScenario scenario, DateTimeOffset startedAt)
    {
        public FaultScenario Scenario { get; set; } = scenario;

        public DateTimeOffset StartedAt { get; } = startedAt;

        public ScenarioMoment Moment { get; set; } = new(false, 0, 0, []);
    }

    /// <summary>Beh scenara; null = nebezi.</summary>
    public ScenarioRunDto? Status(int id)
    {
        lock (_lock)
        {
            if (!_runs.TryGetValue(id, out var run))
                return null;
            return new ScenarioRunDto(run.StartedAt, run.Scenario.LengthMs, run.Moment.Cycle, run.Moment.ActiveSteps.ToArray());
        }
    }

    /// <summary>Spusti scenar od zaciatku (beziaci znova).</summary>
    public async Task StartAsync(int id, FaultScenario scenario)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_lock)
                _runs[id] = new Run(scenario, time.GetUtcNow());
            await ReconcileAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        live.Publish(new ScenariosMessage());
    }

    /// <summary>Beziacemu scenaru zmeni kroky bez zmeny casu spustenia; nebeziaci necha tak.</summary>
    public async Task ReplaceAsync(int id, FaultScenario scenario)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_lock)
            {
                if (!_runs.TryGetValue(id, out var run))
                    return;
                run.Scenario = scenario;
            }

            await ReconcileAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Zastavi scenar a vrati, co nastavil; false = nebezal.</summary>
    public async Task<bool> StopAsync(int id)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_lock)
            {
                if (!_runs.Remove(id))
                    return false;
            }

            await ReconcileAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        live.Publish(new ScenariosMessage());
        return true;
    }

    /// <summary>Zastavi vsetky scenare (pri ukonceni servera - poruchy zo scenara sa nemaju ulozit).</summary>
    public async Task StopAllAsync()
    {
        // server pri teste moze sluzby uvolnit skor, nez zastavi hostovane sluzby
        if (_disposed)
            return;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_lock)
                _runs.Clear();
            await ReconcileAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Posunie scenare v case; pri zmene platnych krokov alebo konci scenara upozorni prehliadace.</summary>
    public async Task TickAsync()
    {
        bool changed;
        if (_disposed)
            return;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (_lock)
            {
                if (_runs.Count == 0)
                    return;
            }

            changed = await ReconcileAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        if (changed)
            live.Publish(new ScenariosMessage());
    }

    /// <summary>Zladi simulator so scenarmi; vrati true, ak niektory scenar zmenil kroky, cyklus alebo dobehol.</summary>
    private async Task<bool> ReconcileAsync()
    {
        var now = time.GetUtcNow();
        var faults = new Dictionary<string, BoardFault>(StringComparer.Ordinal);
        var powerOff = new HashSet<string>(StringComparer.Ordinal);
        var outages = new HashSet<int>();
        var changed = false;
        lock (_lock)
        {
            foreach (var (id, run) in _runs.OrderBy(r => r.Value.StartedAt).ToList())
            {
                var moment = ScenarioTimeline.At(run.Scenario, now - run.StartedAt);
                if (moment.Finished)
                {
                    _runs.Remove(id);
                    changed = true;
                    continue;
                }

                if (moment.Cycle != run.Moment.Cycle || !moment.ActiveSteps.SequenceEqual(run.Moment.ActiveSteps))
                    changed = true;
                run.Moment = moment;
                var effects = ScenarioTimeline.Effects(run.Scenario, moment.ActiveSteps, BoardsOfLine);
                foreach (var (board, fault) in effects.Faults)
                    faults[board] = fault;
                powerOff.UnionWith(effects.PoweredOff);
                outages.UnionWith(effects.Outages);
            }
        }

        ApplyFaults(faults);
        ApplyPower(powerOff);
        if (ApplyOutages(outages))
            await transport.SyncAsync().ConfigureAwait(false);
        return changed;
    }

    private List<string> BoardsOfLine(int line) => simulator.Read(s => s.Boards.Where(b => b.Line == line).Select(b => b.Id).ToList());

    private void ApplyFaults(Dictionary<string, BoardFault> wanted)
    {
        foreach (var (board, fault) in wanted)
        {
            if (_applied.TryGetValue(board, out var applied) && applied == fault)
                continue;
            var current = simulator.Read(s => s.FindBoard(board)?.Fault);
            if (current is null)
                continue;
            _original.TryAdd(board, current);
            simulator.SetFault(board, fault);
            _applied[board] = fault;
        }

        foreach (var board in _applied.Keys.Where(b => !wanted.ContainsKey(b)).ToList())
        {
            simulator.SetFault(board, _original.GetValueOrDefault(board) ?? BoardFault.None);
            _applied.Remove(board);
            _original.Remove(board);
        }
    }

    private void ApplyPower(HashSet<string> wanted)
    {
        foreach (var board in wanted.Where(b => !_poweredOff.Contains(b)))
        {
            if (simulator.SetPower(board, false))
                _poweredOff.Add(board);
        }

        foreach (var board in _poweredOff.Where(b => !wanted.Contains(b)).ToList())
        {
            simulator.SetPower(board, true);
            _poweredOff.Remove(board);
        }
    }

    /// <summary>Vrati true, ak sa vypadok niektorej linky zmenil (prenos treba zladit).</summary>
    private bool ApplyOutages(HashSet<int> wanted)
    {
        var changed = false;
        foreach (var line in wanted.Where(l => !_outages.Contains(l)))
        {
            if (simulator.SetLineOutage(line, true))
            {
                _outages.Add(line);
                changed = true;
            }
        }

        foreach (var line in _outages.Where(l => !wanted.Contains(l)).ToList())
        {
            simulator.SetLineOutage(line, false);
            _outages.Remove(line);
            changed = true;
        }

        return changed;
    }

    public void Dispose()
    {
        _disposed = true;
        _gate.Dispose();
    }
}
