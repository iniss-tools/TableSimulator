using TableSimulator.Core.Boards;
using TableSimulator.Core.Lines;
using TableSimulator.Core.Simulation;

namespace TableSimulator.Core.Protocols;

/// <summary>
/// Co protokol pri spracovani spravy potrebuje od simulatora.
/// </summary>
internal interface ILineContext
{
    /// <summary>Linka, na ktorej sprava prisla.</summary>
    SimLine Line { get; }

    /// <summary>Aktualny cas.</summary>
    DateTimeOffset Now { get; }

    /// <summary>Okolie simulatora (poloha pre snimac tmy, dlzky zvukov).</summary>
    SimEnvironment Environment { get; }

    /// <summary>Tabula na adrese; pri samouceni ju vytvori. null = na adrese nikto nie je.</summary>
    /// <param name="address">adresa</param>
    /// <param name="manufacturer">vyrobca novej tabule zo samoucenia; null = podla linky</param>
    SimBoard? FindBoard(int address, string? manufacturer = null);

    /// <summary>
    /// Zariadenie linky na adrese (napr. rozhlas vzdialeneho INISSu) - ak nie je, vytvori ho aj bez samoucenia.
    /// null = zariadenie je bez napajania.
    /// </summary>
    /// <param name="address">adresa zariadenia</param>
    /// <param name="manufacturer">vyrobca (druh zariadenia)</param>
    SimBoard? Device(int address, string manufacturer);

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
