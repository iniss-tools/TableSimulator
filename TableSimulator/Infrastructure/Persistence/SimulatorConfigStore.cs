using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TableSimulator.Core.Simulation;
using TableSimulator.Persistence.Simulation;

namespace TableSimulator.Infrastructure.Persistence;

/// <summary>
/// Linky a tabule simulatora v databaze - pri starte sa nacitaju, po kazdej zmene sa s kratkym odkladom ulozia cele
/// (viac zmien za sebou sa ulozi raz, ukladania idu za sebou).
/// </summary>
public sealed partial class SimulatorConfigStore : IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly Simulator _simulator;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SimulatorConfigStore> _logger;
    private readonly Timer _timer;
    private readonly SemaphoreSlim _saving = new(1, 1);

    public SimulatorConfigStore(Simulator simulator, IServiceScopeFactory scopes, ILogger<SimulatorConfigStore> logger)
    {
        _simulator = simulator;
        _scopes = scopes;
        _logger = logger;
        _timer = new Timer(_ => _ = SaveAsync(), null, Timeout.Infinite, Timeout.Infinite);
        _simulator.Changed += OnChanged;
    }

    /// <summary>Nacita linky a tabule z databazy do simulatora.</summary>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        SimulatorConfig config;
        await using (var scope = _scopes.CreateAsyncScope())
            config = await scope.ServiceProvider.GetRequiredService<SimulatorConfigRepository>().LoadAsync(cancellationToken).ConfigureAwait(false);
        _simulator.Load(config);
        Loaded(_logger, config.Lines.Count, config.Boards.Count);
    }

    /// <summary>Naplanuje ulozenie.</summary>
    public void ScheduleSave() => _timer.Change(SaveDelay, Timeout.InfiniteTimeSpan);

    /// <summary>Ulozi hned; chybu zapise do zaznamu (casovac nesmie spadnut).</summary>
    public async Task SaveAsync()
    {
        await _saving.WaitAsync().ConfigureAwait(false);
        try
        {
            var config = _simulator.ToConfig();
            await using var scope = _scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<SimulatorConfigRepository>().SaveAsync(config, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception e) when (e is DbUpdateException or SqliteException or InvalidOperationException or ObjectDisposedException)
        {
            SaveFailed(_logger, e);
        }
        finally
        {
            _saving.Release();
        }
    }

    private void OnChanged(SimulatorEvent e)
    {
        if (e is StructureChanged or BoardChanged)
            ScheduleSave();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _simulator.Changed -= OnChanged;
        _timer.Dispose();
        // _saving sa neuvolnuje: posledne ukladanie (pri zastaveni servera) moze este bezat a bez cakacieho handle
        // SemaphoreSlim nic nedrzi
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Linky a tabule z databazy: {Lines} liniek, {Boards} tabul")]
    private static partial void Loaded(ILogger logger, int lines, int boards);

    [LoggerMessage(Level = LogLevel.Error, Message = "Linky a tabule sa nepodarilo ulozit do databazy")]
    private static partial void SaveFailed(ILogger logger, Exception exception);
}
