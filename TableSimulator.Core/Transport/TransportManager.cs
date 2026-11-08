using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Transport;

/// <summary>
/// Drzi servery a zrkadla vsetkych liniek simulatora v sulade s ich nastavenim.
/// </summary>
public sealed class TransportManager : IAsyncDisposable
{
    private readonly Simulator _simulator;
    private readonly ILogger _logger;
    private readonly Dictionary<int, LineHost> _hosts = [];
    private readonly Dictionary<int, SerialMirror> _mirrors = [];
    // posledny vysledok zapisu zrkadla - do zaznamu ide len zmena
    private readonly Dictionary<int, string?> _mirrorErrors = [];
    private readonly Lock _mirrorLock = new();
    private readonly SemaphoreSlim _sync = new(1, 1);

    public TransportManager(Simulator simulator, ILogger<TransportManager>? logger = null)
    {
        _simulator = simulator;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _simulator.Traffic += OnTraffic;
    }

    private void OnTraffic(int line, byte[] data)
    {
        SerialMirror? mirror;
        lock (_mirrorLock)
            mirror = _mirrors.GetValueOrDefault(line);
        if (mirror == null)
            return;

        var error = mirror.Write(data);
        _simulator.SetMirrorError(line, error);
        bool changed;
        lock (_mirrorLock)
        {
            changed = !_mirrorErrors.TryGetValue(line, out var last) || last != error;
            _mirrorErrors[line] = error;
        }

        if (!changed)
            return;
        if (error == null)
            TransportLog.MirrorWriting(_logger, line, mirror.Endpoint);
        else
            TransportLog.MirrorFailed(_logger, line, mirror.Endpoint, error);
    }

    /// <summary>
    /// Spusti servery novych liniek, zastavi servery odstranenych a restartuje linky so zmenenym koncovym bodom.
    /// </summary>
    public async Task SyncAsync()
    {
        await _sync.WaitAsync().ConfigureAwait(false);
        try
        {
            // linka s predstieranym vypadkom nebezi - po jeho skonceni sa spusti znova
            var wanted = _simulator.Read(s => s.Lines.Where(l => !l.Outage).ToDictionary(l => l.Number, l => l.Endpoint));
            var mirrors = _simulator.Read(s => s.Lines.Where(l => l.Mirror != null).ToDictionary(l => l.Number, l => l.Mirror!));
            lock (_mirrorLock)
            {
                foreach (var (number, mirror) in _mirrors.ToList())
                {
                    if (mirrors.TryGetValue(number, out var endpoint) && endpoint == mirror.Endpoint)
                        continue;
                    _mirrors.Remove(number);
                    _mirrorErrors.Remove(number);
                    mirror.Dispose();
                    _simulator.SetMirrorError(number, null);
                }

                foreach (var (number, endpoint) in mirrors)
                    _mirrors.TryAdd(number, new SerialMirror(endpoint));
            }

            foreach (var (number, host) in _hosts.ToList())
            {
                if (wanted.TryGetValue(number, out var endpoint) && endpoint == host.Endpoint)
                    continue;
                _hosts.Remove(number);
                await host.DisposeAsync().ConfigureAwait(false);
            }

            foreach (var (number, endpoint) in wanted)
            {
                if (_hosts.ContainsKey(number))
                    continue;
                var host = new LineHost(_simulator, number, endpoint, _logger);
                _hosts[number] = host;
                host.Start();
            }
        }
        finally
        {
            _sync.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _simulator.Traffic -= OnTraffic;
        foreach (var host in _hosts.Values)
            await host.DisposeAsync().ConfigureAwait(false);
        _hosts.Clear();
        lock (_mirrorLock)
        {
            foreach (var mirror in _mirrors.Values)
                mirror.Dispose();
            _mirrors.Clear();
        }

        _sync.Dispose();
    }
}
