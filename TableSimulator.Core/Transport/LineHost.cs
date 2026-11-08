using System.IO.Pipes;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _statusLock = new();
    private Task? _loop;
    private TcpClient? _client;
    private LineStatus? _status;
    private string? _detail;

    public LineHost(Simulator simulator, int line, LineEndpoint endpoint, ILogger? logger = null)
    {
        _simulator = simulator;
        _logger = logger ?? NullLogger.Instance;
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
            SetStatus(LineStatus.Listening, null);
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
                SetStatus(LineStatus.Listening, null);
                _loop = AcceptLoopAsync(listener, _cts.Token);
            }
            else
            {
                var udp = new UdpClient(new IPEndPoint(address, Endpoint.Port));
                SetStatus(LineStatus.Listening, null);
                _loop = UdpLoopAsync(udp, _cts.Token);
            }
        }
        catch (SocketException e)
        {
            SetStatus(LineStatus.Error, e.Message);
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
        SetStatus(LineStatus.Stopped, null);
    }

    /// <summary>Stav linky v simulatore; do zaznamu ide len zmena (UDP hlasi spojenie pri kazdom datagrame).</summary>
    private void SetStatus(LineStatus status, string? detail)
    {
        _simulator.SetLineStatus(Line, status, detail);
        LineStatus? previous;
        lock (_statusLock)
        {
            previous = _status;
            if (previous == status && _detail == detail)
                return;
            _status = status;
            _detail = detail;
        }

        switch (status)
        {
            case LineStatus.Listening when previous == LineStatus.Connected:
                TransportLog.Disconnected(_logger, Line);
                break;
            case LineStatus.Listening when previous != LineStatus.Listening:
                TransportLog.Listening(_logger, Line, Endpoint);
                break;
            case LineStatus.Connected:
                TransportLog.Connected(_logger, Line, detail);
                break;
            case LineStatus.Error when previous != LineStatus.Error:
                TransportLog.Failed(_logger, Line, Endpoint, detail);
                break;
            case LineStatus.Stopped when _simulator.Read(s => s.FindLine(Line)?.Outage == true):
                TransportLog.Outage(_logger, Line);
                break;
            case LineStatus.Stopped:
                TransportLog.Stopped(_logger, Line);
                break;
        }
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
        SetStatus(LineStatus.Connected, remote);
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
                SetStatus(LineStatus.Listening, null);
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

            SetStatus(LineStatus.Connected, Endpoint.ToString());
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
                SetStatus(LineStatus.Listening, null);
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
                SetStatus(LineStatus.Connected, Endpoint.Host);
                // spinanie zosilnovaca: DTR INISSu je na tomto konci DSR alebo DCD
                if (_simulator.WatchesModemSignal(Line))
                {
                    var watched = port;
                    watched.PinChanged += (_, e) =>
                    {
                        if (e.EventType is SerialPinChange.DsrChanged or SerialPinChange.CDChanged)
                            ModemSignal(watched);
                    };
                    ModemSignal(watched);
                }
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
                SetStatus(LineStatus.Error, e.Message);
                await Task.Delay(SerialRetry, token).ConfigureAwait(false);
            }
            finally
            {
                port?.Dispose();
                // zavrety port = INISS zosilnovac nedrzi
                _simulator.SetModemSignal(Line, false);
            }
        }
    }

    private void ModemSignal(SerialPort port)
    {
        try
        {
            _simulator.SetModemSignal(Line, port.DsrHolding || port.CDHolding);
        }
        catch (Exception e) when (e is InvalidOperationException or IOException)
        {
            // port sa prave zatvara
        }
    }

    private async Task UdpLoopAsync(UdpClient udp, CancellationToken token)
    {
        using (udp)
        {
            while (!token.IsCancellationRequested)
            {
                var result = await udp.ReceiveAsync(token).ConfigureAwait(false);
                SetStatus(LineStatus.Connected, result.RemoteEndPoint.ToString());
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
