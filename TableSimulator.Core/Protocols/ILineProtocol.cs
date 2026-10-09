using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;

namespace TableSimulator.Core.Protocols;

/// <summary>
/// Protokol linky - z prijatych bajtov sklada spravy, meni obsah tabul a odpoveda INISSu.
/// Jedna instancia patri jednej linke.
/// </summary>
internal interface ILineProtocol
{
    /// <summary>Zahodi rozpracovanu spravu (nove spojenie).</summary>
    void Reset();

    /// <summary>Spracuje bajty prijate od INISSu.</summary>
    void Receive(ReadOnlySpan<byte> data, ILineContext context);

    /// <summary>Novy obsah tabule vyrobcu <paramref name="manufacturer" /> (linka jednej rodiny ho neberie do uvahy).</summary>
    BoardDisplay CreateDisplay(string manufacturer);

    /// <summary>
    /// Do zrkadla linky idu spravy tabul, ktore protokol posle cez <see cref="ILineContext.Forward" />, nie prijate bajty
    /// (protokol bajty tabul obaluje).
    /// </summary>
    bool MirrorsInnerFrames => false;

    /// <summary>
    /// Odpoved tabule pri predstieranej poruche <see cref="FaultKind.Nak" />, <see cref="FaultKind.Garbage" /> alebo
    /// <see cref="FaultKind.Status" /> namiesto beznej odpovede <paramref name="normal" />.
    /// </summary>
    (byte[] Bytes, FrameInfo Info) FaultReply(BoardFault fault, byte[] normal);
}

/// <summary>
/// Co protokol pri spracovani spravy potrebuje od simulatora.
/// </summary>
internal interface ILineContext
{
    /// <summary>Linka, na ktorej sprava prisla.</summary>
    SimLine Line { get; }

    /// <summary>Aktualny cas.</summary>
    DateTimeOffset Now { get; }

    /// <summary>Tabula na adrese; pri samouceni ju vytvori. null = na adrese nikto nie je.</summary>
    /// <param name="address">adresa</param>
    /// <param name="manufacturer">vyrobca novej tabule zo samoucenia; null = podla linky</param>
    SimBoard? FindBoard(int address, string? manufacturer = null);

    /// <summary>Vsetky tabule na linke (hromadne spravy).</summary>
    IReadOnlyList<SimBoard> Boards { get; }

    /// <summary>Tabula zmenila obsah.</summary>
    void Changed(SimBoard board);

    /// <summary>Zapise prijatu spravu do zaznamu.</summary>
    void LogReceived(byte[] bytes, int? address, FrameStatus status, FrameInfo info);

    /// <summary>
    /// Odpovie INISSu za tabulu <paramref name="board" /> - podla rezimu linky a poruchy tabule odpoved upravi,
    /// oneskori alebo vynecha.
    /// </summary>
    /// <param name="board">tabula</param>
    /// <param name="bytes">bezna odpoved</param>
    /// <param name="info">vyznam odpovede</param>
    /// <param name="positive">odpoved hovori "v poriadku" (pocitadlo potvrdeni)</param>
    void Reply(SimBoard board, byte[] bytes, FrameInfo info, bool positive = true);

    /// <summary>Odpoved zariadenia linky (napr. sietovy prevodnik) - bez poruch tabule, len podla rezimu linky.</summary>
    /// <param name="address">adresa tabule, ktorej odpoved patri</param>
    /// <param name="bytes">odpoved</param>
    /// <param name="delay">oneskorenie</param>
    /// <param name="status">vysledok</param>
    /// <param name="info">vyznam odpovede</param>
    /// <param name="log">zapisat do zaznamu komunikacie (napr. odpoved na pravidelny dotaz nie)</param>
    void ReplyRaw(int? address, byte[] bytes, TimeSpan delay, FrameStatus status, FrameInfo info, bool log = true);

    /// <summary>Sprava tabule vybalena z obalu linky - pre zrkadlo (<see cref="ILineProtocol.MirrorsInnerFrames" />).</summary>
    void Forward(byte[] frame);
}

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
