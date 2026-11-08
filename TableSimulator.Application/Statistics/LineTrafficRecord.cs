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
