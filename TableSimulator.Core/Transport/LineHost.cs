using System.Globalization;
using System.IO.Pipes;
using System.IO.Ports;
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

    /// <summary>Ako dlho sa caka, kym sa seriovy port skusi otvorit znova.</summary>
    private static readonly TimeSpan SerialRetry = TimeSpan.FromSeconds(2);

    /// <summary>Spusti server; chybu (obsadeny port) zapise do stavu linky.</summary>
    public void Start()
    {
        // stav pred spustenim slucky - rychle spojenie ho inak prepise
        if (Endpoint.Transport == LineTransport.Pipe)
        {
            _simulator.SetLineStatus(Line, LineStatus.Listening, null);
            _loop = PipeLoopAsync(_cts.Token);
            return;
        }

        if (Endpoint.Transport == LineTransport.Serial)
        {
            _loop = SerialLoopAsync(_cts.Token);
            return;
        }

        try
        {
            var address = ParseAddress(Endpoint.Host);
            if (Endpoint.Transport == LineTransport.Tcp)
            {
                var listener = new TcpListener(address, Endpoint.Port);
                listener.Start();
                _simulator.SetLineStatus(Line, LineStatus.Listening, null);
                _loop = AcceptLoopAsync(listener, _cts.Token);
            }
            else
            {
                var udp = new UdpClient(new IPEndPoint(address, Endpoint.Port));
                _simulator.SetLineStatus(Line, LineStatus.Listening, null);
                _loop = UdpLoopAsync(udp, _cts.Token);
            }
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

    private async Task SerialLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];
        while (!token.IsCancellationRequested)
        {
            SerialPort? port = null;
            try
            {
                port = SerialPorts.Open(Endpoint);
                // citanie zo serioveho portu nereaguje na zrusenie - zatvorenie portu ho prerusi
                using var cancel = token.Register(port.Dispose);
                _simulator.SetLineStatus(Line, LineStatus.Connected, Endpoint.Host);
                var stream = port.BaseStream;
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
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException
                                          or ObjectDisposedException)
            {
                if (token.IsCancellationRequested)
                    break;
                _simulator.SetLineStatus(Line, LineStatus.Error, e.Message);
                await Task.Delay(SerialRetry, token).ConfigureAwait(false);
            }
            finally
            {
                port?.Dispose();
            }
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
/// Otvaranie seriovych portov podla koncoveho bodu linky.
/// </summary>
public static class SerialPorts
{
    /// <summary>Otvori seriovy port <c>COMn:rychlost,parita,data,stop</c>.</summary>
    public static SerialPort Open(LineEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var parts = endpoint.Pipe.Split(',');
        var parity = (parts.ElementAtOrDefault(0) ?? "n") switch
        {
            "o" => Parity.Odd,
            "e" => Parity.Even,
            "m" => Parity.Mark,
            "s" => Parity.Space,
            _ => Parity.None
        };
        var dataBits = int.TryParse(parts.ElementAtOrDefault(1), NumberStyles.None, CultureInfo.InvariantCulture, out var d) ? d : 8;
        var stopBits = parts.ElementAtOrDefault(2) switch
        {
            "2" => StopBits.Two,
            "1.5" => StopBits.OnePointFive,
            _ => StopBits.One
        };
        var port = new SerialPort(endpoint.Host, endpoint.Port, parity, dataBits, stopBits);
        port.Open();
        return port;
    }
}

/// <summary>
/// Zrkadlo linky: kopiruje spravy od INISSu na seriovy port (napr. do ELEN Monitora cez virtualny par COM portov),
/// co pride spat, zahodi. Pri chybe port zatvori a skusi ho znova otvorit pri dalsej sprave, najskor po 2 s.
/// </summary>
internal sealed class SerialMirror : IDisposable
{
    private static readonly TimeSpan Retry = TimeSpan.FromSeconds(2);

    private readonly Lock _lock = new();
    private SerialPort? _port;
    private DateTime _nextTry = DateTime.MinValue;
    private string? _error;

    public SerialMirror(LineEndpoint endpoint)
    {
        Endpoint = endpoint;
    }

    public LineEndpoint Endpoint { get; }

    /// <summary>Zapise bajty; vrati poslednu chybu (aj pocas cakania na dalsi pokus) alebo null.</summary>
    public string? Write(byte[] data)
    {
        lock (_lock)
        {
            try
            {
                if (_port == null)
                {
                    if (DateTime.UtcNow < _nextTry)
                        return _error;
                    _port = SerialPorts.Open(Endpoint);
                    _port.DataReceived += (_, _) => Discard();
                }

                _port.Write(data, 0, data.Length);
                return _error = null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException
                                          or TimeoutException)
            {
                _port?.Dispose();
                _port = null;
                _nextTry = DateTime.UtcNow + Retry;
                return _error = e.Message;
            }
        }
    }

    private void Discard()
    {
        lock (_lock)
        {
            try
            {
                _port?.DiscardInBuffer();
            }
            catch (Exception e) when (e is IOException or InvalidOperationException)
            {
                // port sa prave zatvara
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _port?.Dispose();
            _port = null;
        }
    }
}

/// <summary>
/// Drzi servery a zrkadla vsetkych liniek simulatora v sulade s ich nastavenim.
/// </summary>
public sealed class TransportManager : IAsyncDisposable
{
    private readonly Simulator _simulator;
    private readonly Dictionary<int, LineHost> _hosts = [];
    private readonly Dictionary<int, SerialMirror> _mirrors = [];
    private readonly Lock _mirrorLock = new();
    private readonly SemaphoreSlim _sync = new(1, 1);

    public TransportManager(Simulator simulator)
    {
        _simulator = simulator;
        _simulator.Traffic += OnTraffic;
    }

    private void OnTraffic(int line, byte[] data)
    {
        SerialMirror? mirror;
        lock (_mirrorLock)
            mirror = _mirrors.GetValueOrDefault(line);
        if (mirror != null)
            _simulator.SetMirrorError(line, mirror.Write(data));
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
            var mirrors = _simulator.Read(s => s.Lines.Where(l => l.Mirror != null).ToDictionary(l => l.Number, l => l.Mirror!));
            lock (_mirrorLock)
            {
                foreach (var (number, mirror) in _mirrors.ToList())
                {
                    if (mirrors.TryGetValue(number, out var endpoint) && endpoint == mirror.Endpoint)
                        continue;
                    _mirrors.Remove(number);
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
