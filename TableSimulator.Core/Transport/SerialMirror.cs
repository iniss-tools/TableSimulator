using System.IO.Ports;
using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Transport;

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
