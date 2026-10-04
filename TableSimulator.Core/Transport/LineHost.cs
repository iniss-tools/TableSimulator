using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Transport;

/// <summary>
/// Spojenie jednej linky s INISSom. Pri TCP a UDP je simulator server a INISS sa pripaja (<c>TablePort=N=TCP://…</c>);
/// pri rure (<c>TablePort=N=\\.\PIPE\meno</c>) ruru vytvori INISS a simulator sa k nej pripaja ako klient, kym INISS
/// nebezi, skusa to znova. Prijate bajty idu do simulatora a jeho odpovede spat. Pri TCP plati vzdy posledne spojenie -
/// nove spojenie stare zatvori.
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

    /// <summary>Ako dlho sa caka na ruru INISSu, kym sa pokus zopakuje.</summary>
    private static readonly TimeSpan PipeRetry = TimeSpan.FromSeconds(1);

    /// <summary>Spusti server; chybu (obsadeny port) zapise do stavu linky.</summary>
    public void Start()
    {
        if (Endpoint.Transport == LineTransport.Pipe)
        {
            _loop = PipeLoopAsync(_cts.Token);
            _simulator.SetLineStatus(Line, LineStatus.Listening, null);
            return;
        }

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

    private async Task PipeLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];
        while (!token.IsCancellationRequested)
        {
            using var pipe = new NamedPipeClientStream(Endpoint.Host, Endpoint.Pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(PipeRetry, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException)
            {
                // INISS este rura nevytvoril alebo su vsetky jej instancie obsadene
                await Task.Delay(PipeRetry, token).ConfigureAwait(false);
                continue;
            }

            // INISS ruru vytvara v rezime sprav - kazda odpoved ide ako samostatna sprava
            if (OperatingSystem.IsWindows())
                pipe.ReadMode = PipeTransmissionMode.Message;

            _simulator.SetLineStatus(Line, LineStatus.Connected, Endpoint.ToString());
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var read = await pipe.ReadAsync(buffer, token).ConfigureAwait(false);
                    if (read == 0)
                        break;

                    foreach (var reply in _simulator.Receive(Line, buffer.AsSpan(0, read)))
                    {
                        if (reply.Delay > TimeSpan.Zero)
                            await Task.Delay(reply.Delay, token).ConfigureAwait(false);
                        await pipe.WriteAsync(reply.Bytes, token).ConfigureAwait(false);
                    }
                }
            }
            catch (IOException)
            {
                // INISS ruru zavrel
            }

            if (!token.IsCancellationRequested)
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
