using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Api;

/// <summary>
/// Zivy prenos zmien do prehliadacov cez WebSocket. Zmeny tabul a liniek sa zlucuju (najviac 20× za sekundu),
/// zaznamy komunikacie idu len klientom, ktori o ne poziadali (<c>/ws?log=1</c>).
/// </summary>
public sealed class LiveHub : IDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(50);

    private readonly Simulator _simulator;
    private readonly Func<string?> _connectHost;
    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private readonly Channel<SimulatorEvent> _events = Channel.CreateUnbounded<SimulatorEvent>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;

    public LiveHub(Simulator simulator, Func<string?> connectHost)
    {
        _simulator = simulator;
        _connectHost = connectHost;
        _simulator.Changed += OnChanged;
        _pump = PumpAsync(_cts.Token);
    }

    /// <summary>Posle vsetkym klientom spravu mimo simulatora (napr. zmena vzhladu).</summary>
    public void Broadcast(object message) => _ = SendAllAsync(JsonSerializer.SerializeToUtf8Bytes(message, AppSettings.Json), false);

    /// <summary>Obsluzi jedno spojenie WebSocket az do jeho zatvorenia.</summary>
    public async Task ServeAsync(WebSocket socket, bool wantsLog, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(socket);
        var id = Guid.NewGuid();
        var client = new Client(socket, wantsLog);
        _clients[id] = client;
        try
        {
            var buffer = new byte[256];
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (Exception e) when (e is WebSocketException or OperationCanceledException)
        {
            // prehliadac odisiel
        }
        finally
        {
            _clients.TryRemove(id, out _);
            client.Dispose();
        }
    }

    private void OnChanged(SimulatorEvent e) => _events.Writer.TryWrite(e);

    private async Task PumpAsync(CancellationToken token)
    {
        var boards = new HashSet<string>();
        var lines = new HashSet<int>();
        var frames = new List<FrameDto>();
        try
        {
            while (await _events.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                var structure = false;
                while (_events.Reader.TryRead(out var e))
                {
                    switch (e)
                    {
                        case BoardChanged b: boards.Add(b.BoardId); break;
                        case LineChanged l: lines.Add(l.Line); break;
                        case StructureChanged: structure = true; break;
                        case FrameLogged f: frames.Add(Dto.From(f.Record)); break;
                    }
                }

                if (structure)
                    await SendAllAsync(Serialize(new { type = "structure" }), false).ConfigureAwait(false);

                if (boards.Count > 0 || lines.Count > 0)
                {
                    var host = _connectHost();
                    var (boardDtos, lineDtos) = _simulator.Read(s => (
                        boards.Select(s.FindBoard).OfType<Core.Boards.SimBoard>().Select(Dto.From).ToArray(),
                        lines.Select(s.FindLine).OfType<Core.Lines.SimLine>().Select(l => Dto.From(l, host)).ToArray()));
                    foreach (var board in boardDtos)
                        await SendAllAsync(Serialize(new { type = "board", board }), false).ConfigureAwait(false);
                    foreach (var line in lineDtos)
                        await SendAllAsync(Serialize(new { type = "line", line }), false).ConfigureAwait(false);
                }

                if (frames.Count > 0)
                    await SendAllAsync(Serialize(new { type = "frames", frames }), true).ConfigureAwait(false);

                boards.Clear();
                lines.Clear();
                frames.Clear();
                await Task.Delay(Interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // koniec servera
        }
    }

    private static byte[] Serialize(object message) => JsonSerializer.SerializeToUtf8Bytes(message, AppSettings.Json);

    private async Task SendAllAsync(byte[] data, bool logOnly)
    {
        foreach (var client in _clients.Values)
        {
            if (logOnly && !client.WantsLog)
                continue;
            await client.SendAsync(data).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _simulator.Changed -= OnChanged;
        _cts.Cancel();
        _events.Writer.TryComplete();
        try
        {
            _pump.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // pumpa skoncila zrusenim
        }

        _cts.Dispose();
    }

    private sealed class Client(WebSocket socket, bool wantsLog) : IDisposable
    {
        private readonly SemaphoreSlim _send = new(1, 1);

        public bool WantsLog => wantsLog;

        public async Task SendAsync(byte[] data)
        {
            if (socket.State != WebSocketState.Open)
                return;
            await _send.WaitAsync().ConfigureAwait(false);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await socket.SendAsync(data, WebSocketMessageType.Text, true, timeout.Token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is WebSocketException or OperationCanceledException or ObjectDisposedException)
            {
                // pomaly alebo odpojeny klient - dalsia zmena pride znova
            }
            finally
            {
                _send.Release();
            }
        }

        public void Dispose() => _send.Dispose();
    }
}
