using TableSimulator.Core.Statistics;

namespace TableSimulator.Application.Statistics;

/// <summary>Komunikacia tabule za minutu v databaze.</summary>
public sealed class BoardTrafficRecord
{
    public int Line { get; set; }
    public int Address { get; set; }

    /// <summary>Minuta od 1. 1. 1970 UTC; v dotaze zaciatok intervalu.</summary>
    public long Minute { get; set; }

    public int Frames { get; set; }
    public int Acks { get; set; }
    public int Naks { get; set; }
    public int Unanswered { get; set; }
    public long? MaxGapMs { get; set; }

    public static BoardTrafficRecord From(BoardMinute m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return new BoardTrafficRecord
        {
            Line = m.Line, Address = m.Address, Minute = m.Minute, Frames = m.Frames, Acks = m.Acks, Naks = m.Naks, Unanswered = m.Unanswered,
            MaxGapMs = m.MaxGapMs
        };
    }

    /// <summary>Pripocita dalsi zapis tej istej minuty.</summary>
    public void Add(BoardMinute m)
    {
        ArgumentNullException.ThrowIfNull(m);
        Frames += m.Frames;
        Acks += m.Acks;
        Naks += m.Naks;
        Unanswered += m.Unanswered;
        if (m.MaxGapMs is { } gap && (MaxGapMs is not { } max || gap > max))
            MaxGapMs = gap;
    }
}
