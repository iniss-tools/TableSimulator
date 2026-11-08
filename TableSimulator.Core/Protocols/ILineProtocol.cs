using TableSimulator.Core.Boards;

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
