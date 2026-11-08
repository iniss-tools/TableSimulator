using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols;

/// <summary>
/// Predstierana porucha tabule pri odpovedi.
/// </summary>
internal static class BoardFaults
{
    /// <summary>
    /// Odpoved tabule podla jej poruchy a zapis do pocitadiel; <c>Bytes</c> null = tabula neodpovie.
    /// </summary>
    public static (byte[]? Bytes, TimeSpan Delay, FrameStatus Status, FrameInfo Info) Apply(
        SimBoard board, ILineProtocol protocol, byte[] bytes, FrameInfo info, bool positive)
    {
        var fault = board.Fault;
        var status = FrameStatus.Ok;
        var delay = TimeSpan.Zero;
        switch (fault.Kind)
        {
            case FaultKind.NoReply:
                board.Stats.Unanswered++;
                return (null, delay, FrameStatus.Fault, new FrameInfo("fault.noReply"));
            case FaultKind.Nak or FaultKind.Garbage or FaultKind.Status:
                (bytes, info) = protocol.FaultReply(fault, bytes);
                status = FrameStatus.Fault;
                positive = false;
                break;
            case FaultKind.Delay:
                delay = TimeSpan.FromMilliseconds(Math.Max(0, fault.DelayMs));
                status = FrameStatus.Fault;
                break;
        }

        if (positive)
            board.Stats.Acks++;
        else
            board.Stats.Naks++;
        return (bytes, delay, status, info);
    }
}
