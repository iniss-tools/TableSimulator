using System.Text;
using System.Threading.Channels;
using TableSimulator.Core.Protocols;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Prebiehajuci zaznam komunikacie - simulator mu odovzdava prevadzku pod svojim zamkom, zapis do suboru bezi mimo neho.
/// </summary>
public sealed class SessionRecorder : IAsyncDisposable
{
    private readonly Channel<RecordingEntry> _queue = Channel.CreateUnbounded<RecordingEntry>(new UnboundedChannelOptions { SingleReader = true });
    private readonly StreamWriter _writer;
    private readonly Task _pump;
    private long _entries;
    private long _bytes;

    /// <param name="output">kam sa zaznam pise (recorder ho na konci zatvori)</param>
    /// <param name="config">linky a tabule pri zaciatku</param>
    /// <param name="started">zaciatok zaznamu</param>
    /// <param name="note">poznamka obsluhy</param>
    public SessionRecorder(Stream output, SimulatorConfig config, DateTimeOffset started, string? note)
    {
        ArgumentNullException.ThrowIfNull(output);
        Started = started;
        Note = note;
        _writer = new StreamWriter(output, new UTF8Encoding(false)) { NewLine = "\n" };
        _writer.WriteLine(RecordingFile.HeaderLine(new RecordingHeader(RecordingFile.FormatName, RecordingFile.Version, started, config, note)));
        _pump = PumpAsync();
    }

    /// <summary>Zaciatok zaznamu.</summary>
    public DateTimeOffset Started { get; }

    /// <summary>Poznamka obsluhy.</summary>
    public string? Note { get; }

    /// <summary>Pocet zaznamenanych kusov prevadzky.</summary>
    public long Entries => Interlocked.Read(ref _entries);

    /// <summary>Pocet zaznamenanych bajtov.</summary>
    public long Bytes => Interlocked.Read(ref _bytes);

    /// <summary>Prida prevadzku (vola simulator).</summary>
    internal void Add(int line, FrameDirection direction, ReadOnlySpan<byte> data, DateTimeOffset now)
    {
        if (data.IsEmpty)
            return;
        Interlocked.Increment(ref _entries);
        Interlocked.Add(ref _bytes, data.Length);
        _queue.Writer.TryWrite(new RecordingEntry(Math.Max(0, (now - Started).TotalSeconds), line, direction, data.ToArray()));
    }

    private async Task PumpAsync()
    {
        while (await _queue.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (_queue.Reader.TryRead(out var entry))
                await _writer.WriteLineAsync(RecordingFile.EntryLine(entry)).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Ukonci zaznam: dopise vsetko a zatvori subor.</summary>
    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        await _pump.ConfigureAwait(false);
        await _writer.DisposeAsync().ConfigureAwait(false);
    }
}
