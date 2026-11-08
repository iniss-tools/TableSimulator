using TableSimulator.Core.Protocols;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Kus prevadzky na linke - bajty tak, ako prisli (nie cele spravy), aby prehratie preslo tym istym skladanim sprav.
/// </summary>
/// <param name="Time">cas od zaciatku zaznamu [s]</param>
/// <param name="Line">cislo linky</param>
/// <param name="Direction">od INISSu (prehrava sa) alebo odpoved tabule (len na porovnanie)</param>
/// <param name="Bytes">bajty</param>
public sealed record RecordingEntry(double Time, int Line, FrameDirection Direction, byte[] Bytes);
