using System.Net;
using System.Net.Sockets;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Transport;

/// <summary>
/// Sietovy server jednej linky: INISS sa pripaja ako klient (<c>TablePort=N=TCP://…</c>), prijate bajty idu do
/// simulatora a jeho odpovede spat. Pri TCP plati vzdy posledne spojenie - nove spojenie stare zatvori.
/// </summary>
public sealed class LineHost : IAsyncDisposable
{
    private readonly Simulator _simulator;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private TcpClient? _client;

    public LineHost(Simulator simulator, int line, LineEndpoint endpoint)
    {
        _simulator = simulator;
        Line = line;
        Endpoint = endpoint;
    }

    /// <summary>Cislo linky.</summary>
    public int Line { get; }

    /// <summary>Kde server pocuva.</summary>
    public LineEndpoint Endpoint { get; }

    /// <summary>Spusti server; chybu (obsadeny port) zapise do stavu linky.</summary>
    public void Start()
    {
        try
        {
            var address = ParseAddress(Endpoint.Host);
            if (Endpoint.Transport == LineTransport.Tcp)
            {
                var listener = new TcpListener(address, Endpoint.Port);
                listener.Start();
                _loop = AcceptLoopAsync(listener, _cts.Token);
            }
            else
            {
                var udp = new UdpClient(new IPEndPoint(address, Endpoint.Port));
                _loop = UdpLoopAsync(udp, _cts.Token);
            }

            _simulator.SetLineStatus(Line, LineStatus.Listening, null);
        }
        catch (SocketException e)
        {
            _simulator.SetLineStatus(Line, LineStatus.Error, e.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        _client?.Dispose();
        if (_loop != null)
        {
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // zastavenie servera
            }
        }

        _cts.Dispose();
        _simulator.SetLineStatus(Line, LineStatus.Stopped, null);
    }

    private static IPAddress ParseAddress(string host) =>
        host is "" or "*" or "0.0.0.0" ? IPAddress.Any :
        host is "::" ? IPAddress.IPv6Any :
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? IPAddress.Loopback :
        IPAddress.Parse(host);

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
                client.NoDelay = true;
                var previous = Interlocked.Exchange(ref _client, client);
                previous?.Dispose();
                _ = ServeAsync(client, token);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken token)
    {
        var remote = client.Client.RemoteEndPoint?.ToString();
        _simulator.SetLineStatus(Line, LineStatus.Connected, remote);
        var buffer = new byte[4096];
        try
        {
            var stream = client.GetStream();
            while (!token.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                if (read == 0)
                    break;

                foreach (var reply in _simulator.Receive(Line, buffer.AsSpan(0, read)))
                {
                    if (reply.Delay > TimeSpan.Zero)
                        await Task.Delay(reply.Delay, token).ConfigureAwait(false);
                    await stream.WriteAsync(reply.Bytes, token).ConfigureAwait(false);
                }
            }
        }
        catch (Exception e) when (e is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // klient sa odpojil alebo ho nahradilo nove spojenie
        }
        finally
        {
            client.Dispose();
            if (Interlocked.CompareExchange(ref _client, null, client) == client && !token.IsCancellationRequested)
                _simulator.SetLineStatus(Line, LineStatus.Listening, null);
        }
    }

    private async Task UdpLoopAsync(UdpClient udp, CancellationToken token)
    {
        using (udp)
        {
            while (!token.IsCancellationRequested)
            {
                var result = await udp.ReceiveAsync(token).ConfigureAwait(false);
                _simulator.SetLineStatus(Line, LineStatus.Connected, result.RemoteEndPoint.ToString());
                foreach (var reply in _simulator.Receive(Line, result.Buffer))
                {
                    if (reply.Delay > TimeSpan.Zero)
                        await Task.Delay(reply.Delay, token).ConfigureAwait(false);
                    await udp.SendAsync(reply.Bytes, result.RemoteEndPoint, token).ConfigureAwait(false);
                }
            }
        }
    }
}

/// <summary>
/// Drzi servery vsetkych liniek simulatora v sulade s ich nastavenim.
/// </summary>
public sealed class TransportManager : IAsyncDisposable
{
    private readonly Simulator _simulator;
    private readonly Dictionary<int, LineHost> _hosts = [];
    private readonly SemaphoreSlim _sync = new(1, 1);

    public TransportManager(Simulator simulator)
    {
        _simulator = simulator;
    }

    /// <summary>
    /// Spusti servery novych liniek, zastavi servery odstranenych a restartuje linky so zmenenym koncovym bodom.
    /// </summary>
    public async Task SyncAsync()
    {
        await _sync.WaitAsync().ConfigureAwait(false);
        try
        {
            var wanted = _simulator.Read(s => s.Lines.ToDictionary(l => l.Number, l => l.Endpoint));
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
                var host = new LineHost(_simulator, number, endpoint);
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
        foreach (var host in _hosts.Values)
            await host.DisposeAsync().ConfigureAwait(false);
        _hosts.Clear();
        _sync.Dispose();
    }
}
