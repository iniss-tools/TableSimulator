namespace TableSimulator.Core.Protocols.AkSignal;

/// <summary>
/// Sklada prikazy AK Signal UH z prudu bajtov: od <c>&lt;</c> po <c>&gt;</c>. Bajty mimo sprav a prilis dlhu
/// rozpracovanu spravu zahodi.
/// </summary>
public sealed class AkSignalUhFramer
{
    /// <summary>Najdlhsia sprava - zoznam zvukov hlasenia sa zmesti s rezervou.</summary>
    public const int MaxLength = 16 * 1024;

    private readonly List<byte> _buffer = [];

    /// <summary>Pocet bajtov mimo sprav (sum na linke), ktore sa zahodili.</summary>
    public long DiscardedBytes { get; private set; }

    /// <summary>Pocet bajtov rozpracovanej spravy.</summary>
    public int Pending => _buffer.Count;

    /// <summary>Zahodi rozpracovanu spravu.</summary>
    public void Reset() => _buffer.Clear();

    /// <summary>Zahodi rozpracovanu spravu ako sum (po medzere v prijme).</summary>
    public void DropPending()
    {
        DiscardedBytes += _buffer.Count;
        _buffer.Clear();
    }

    /// <summary>Prida prijate bajty a vrati prikazy, ktore sa nimi dokoncili.</summary>
    public IReadOnlyList<AkSignalUhFrame> Push(ReadOnlySpan<byte> data)
    {
        List<AkSignalUhFrame>? frames = null;
        foreach (var b in data)
        {
            if (b == (byte)'<')
            {
                // nova sprava pred koncom predchadzajucej - zaciatok bol sum
                DropPending();
                _buffer.Add(b);
                continue;
            }

            if (_buffer.Count == 0)
            {
                DiscardedBytes++;
                continue;
            }

            _buffer.Add(b);
            if (b == (byte)'>')
            {
                (frames ??= []).Add(AkSignalUhFrame.Parse([.. _buffer]));
                _buffer.Clear();
            }
            else if (_buffer.Count > MaxLength)
            {
                DropPending();
            }
        }

        return frames ?? (IReadOnlyList<AkSignalUhFrame>)[];
    }
}
