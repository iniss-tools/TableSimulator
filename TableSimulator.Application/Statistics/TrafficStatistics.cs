using TableSimulator.Core.Statistics;

namespace TableSimulator.Application.Statistics;

/// <summary>Komunikacia linky za minutu v databaze (sucty sa pri opakovanom zapise tej istej minuty scitaju).</summary>
public sealed class LineTrafficRecord
{
    public int Line { get; set; }

    /// <summary>Minuta od 1. 1. 1970 UTC; v dotaze zaciatok intervalu.</summary>
    public long Minute { get; set; }

    public int Ok { get; set; }
    public int BadChecksum { get; set; }
    public int NoBoard { get; set; }
    public int Noise { get; set; }
    public int Other { get; set; }
    public int Replies { get; set; }
    public int Faults { get; set; }
    public long BytesIn { get; set; }
    public long BytesOut { get; set; }
    public int Connections { get; set; }
    public int Disconnections { get; set; }

    public static LineTrafficRecord From(LineMinute m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return new LineTrafficRecord
        {
            Line = m.Line, Minute = m.Minute, Ok = m.Ok, BadChecksum = m.BadChecksum, NoBoard = m.NoBoard, Noise = m.Noise, Other = m.Other,
            Replies = m.Replies, Faults = m.Faults, BytesIn = m.BytesIn, BytesOut = m.BytesOut, Connections = m.Connections,
            Disconnections = m.Disconnections
        };
    }

    /// <summary>Pripocita dalsi zapis tej istej minuty.</summary>
    public void Add(LineMinute m)
    {
        ArgumentNullException.ThrowIfNull(m);
        Ok += m.Ok;
        BadChecksum += m.BadChecksum;
        NoBoard += m.NoBoard;
        Noise += m.Noise;
        Other += m.Other;
        Replies += m.Replies;
        Faults += m.Faults;
        BytesIn += m.BytesIn;
        BytesOut += m.BytesOut;
        Connections += m.Connections;
        Disconnections += m.Disconnections;
    }
}

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

/// <summary>Zmena stavu linky v databaze; stav je kluc z JSON (<c>connected</c>, <c>lost</c>…).</summary>
public sealed record LineStatusRecord(long Id, int Line, long TimeMs, string Status, string? Detail);

/// <summary>Co dotaz na statistiku vrati z databazy - intervaly s komunikaciou a zmeny stavu liniek.</summary>
public sealed record TrafficData(IReadOnlyList<LineTrafficRecord> Lines, IReadOnlyList<BoardTrafficRecord> Boards, IReadOnlyList<LineStatusRecord> StatusChanges);

/// <summary>Statistika komunikacie v databaze.</summary>
public interface ITrafficStatisticsRepository
{
    /// <summary>Zapise odobraty vzorku; minuta, ktora uz v databaze je, sa pripocita.</summary>
    Task AddAsync(TrafficSample sample, CancellationToken cancellationToken);

    /// <summary>
    /// Intervaly po <paramref name="bucketMinutes" /> minutach v obdobi [<paramref name="fromMinute" />,
    /// <paramref name="toMinute" />) a zmeny stavu liniek v nom aj posledna pred nim; <paramref name="line" /> null = vsetky linky.
    /// </summary>
    Task<TrafficData> QueryAsync(long fromMinute, long toMinute, int bucketMinutes, int? line, CancellationToken cancellationToken);

    /// <summary>Odstrani statistiku starsiu nez <paramref name="minute" />; vrati pocet odstranenych zaznamov.</summary>
    Task<int> DeleteOlderThanAsync(long minute, CancellationToken cancellationToken);
}

/// <summary>Uchovavanie a intervaly grafov statistiky.</summary>
public static class TrafficRetention
{
    /// <summary>Ako dlho sa statistika uchovava [dni].</summary>
    public const int Days = 30;

    /// <summary>Najdlhsie obdobie jedneho dotazu [dni].</summary>
    public const int MaxSpanDays = 31;

    /// <summary>Dlzka intervalu grafu podla obdobia - do 3 h po minute, do 2 dni po 10 min, do 8 dni po hodine, inak po 6 h.</summary>
    public static int BucketFor(TimeSpan span) =>
        span <= TimeSpan.FromHours(3) ? 1 : span <= TimeSpan.FromDays(2) ? 10 : span <= TimeSpan.FromDays(8) ? 60 : 360;
}
