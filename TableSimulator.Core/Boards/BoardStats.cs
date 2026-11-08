namespace TableSimulator.Core.Boards;

/// <summary>
/// Pocitadla komunikacie tabule.
/// </summary>
public sealed class BoardStats
{
    private DateTimeOffset? _lastFrameAt;
    private long? _maxGapMs;

    public long Frames { get; set; }
    public long Acks { get; set; }
    public long Naks { get; set; }
    public long Unanswered { get; set; }

    /// <summary>Posledna sprava pre tabulu; nova hodnota zaznamena pauzu od predchadzajucej (statistika).</summary>
    public DateTimeOffset? LastFrameAt
    {
        get => _lastFrameAt;
        set
        {
            if (_lastFrameAt is { } last && value is { } now && now > last)
            {
                var gap = (long)(now - last).TotalMilliseconds;
                if (_maxGapMs is not { } max || gap > max)
                    _maxGapMs = gap;
            }

            _lastFrameAt = value;
        }
    }

    /// <summary>Stav pocitadiel - prehratie naprazdno ich na konci vrati.</summary>
    internal (long Frames, long Acks, long Naks, long Unanswered, DateTimeOffset? LastFrameAt, long? MaxGapMs) Save() =>
        (Frames, Acks, Naks, Unanswered, _lastFrameAt, _maxGapMs);

    /// <summary>Vrati pocitadla ulozene <see cref="Save" /> (pauza sa nezapocita).</summary>
    internal void Restore((long Frames, long Acks, long Naks, long Unanswered, DateTimeOffset? LastFrameAt, long? MaxGapMs) saved)
    {
        (Frames, Acks, Naks, Unanswered, _lastFrameAt, _maxGapMs) = saved;
    }

    /// <summary>Najdlhsia pauza medzi spravami od posledneho odberu [ms] a jej vynulovanie; null = ziadna.</summary>
    internal long? TakeMaxGap()
    {
        var gap = _maxGapMs;
        _maxGapMs = null;
        return gap;
    }
}
