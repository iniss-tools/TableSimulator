using System.Globalization;
using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Frames;

/// <summary>Chyby vlozenia spravy.</summary>
public static class SendFrameErrors
{
    public static Error InissConnected(int line) =>
        Error.Conflict("frames.inissConnected", string.Create(CultureInfo.InvariantCulture, $"INISS is connected to line {line}; frames can be sent only to a line without INISS."));

    public static Error LineOutage(int line) =>
        Error.Conflict("frames.lineOutage", string.Create(CultureInfo.InvariantCulture, $"Line {line} has a simulated outage."));
}
