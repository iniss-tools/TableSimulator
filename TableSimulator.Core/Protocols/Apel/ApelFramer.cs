namespace TableSimulator.Core.Protocols.Apel;

/// <summary>
/// Sklada spravy APEL z prudu bajtov: zaciatok <c>10 02</c>, koniec <c>10 03</c>, <c>10 10</c> vnutri = jeden bajt
/// <c>10</c>. Bajty mimo sprav zahodi.
/// </summary>
public sealed class ApelFramer
{
    private const int MaxFrameLength = 1024;

    private readonly List<byte> _raw = [];
    private readonly List<byte> _payload = [];
    private bool _inFrame;
    private bool _dle;

    /// <summary>Pocet bajtov mimo sprav (sum na linke), ktore sa zahodili.</summary>
    public long DiscardedBytes { get; private set; }

    /// <summary>Pocet bajtov rozpracovanej spravy.</summary>
    public int Pending => _raw.Count;

    /// <summary>Zahodi rozpracovanu spravu.</summary>
    public void Reset()
    {
        _raw.Clear();
        _payload.Clear();
        _inFrame = false;
        _dle = false;
    }

    /// <summary>Zahodi rozpracovanu spravu ako sum (po medzere v prijme).</summary>
    public void DropPending()
    {
        DiscardedBytes += _raw.Count;
        Reset();
    }

    /// <summary>Prida prijate bajty a vrati spravy, ktore sa nimi dokoncili (aj so zlym suctom).</summary>
    public IReadOnlyList<ApelFrame> Push(ReadOnlySpan<byte> data)
    {
        List<ApelFrame>? frames = null;
        foreach (var b in data)
        {
            if (!_inFrame)
            {
                // cakanie na 10 02
                if (_dle && b == ApelFrame.Stx)
                {
                    _inFrame = true;
                    _dle = false;
                    _raw.Clear();
                    _raw.AddRange([ApelFrame.Dle, ApelFrame.Stx]);
                    _payload.Clear();
                    continue;
                }

                if (_dle)
                    DiscardedBytes++;
                _dle = b == ApelFrame.Dle;
                if (!_dle)
                    DiscardedBytes++;
                continue;
            }

            _raw.Add(b);
            if (_dle)
            {
                _dle = false;
                if (b == ApelFrame.Dle)
                {
                    _payload.Add(ApelFrame.Dle);
                }
                else if (b == ApelFrame.Etx)
                {
                    (frames ??= []).Add(ApelFrame.Parse(_payload.ToArray(), _raw.ToArray()));
                    Reset();
                }
                else if (b == ApelFrame.Stx)
                {
                    // nova sprava bez konca predchadzajucej
                    DiscardedBytes += _raw.Count - 2;
                    _raw.Clear();
                    _raw.AddRange([ApelFrame.Dle, ApelFrame.Stx]);
                    _payload.Clear();
                }
                else
                {
                    _payload.Add(b);
                }

                continue;
            }

            if (b == ApelFrame.Dle)
                _dle = true;
            else
                _payload.Add(b);
            if (_raw.Count > MaxFrameLength)
                DropPending();
        }

        return frames ?? (IReadOnlyList<ApelFrame>)[];
    }
}
