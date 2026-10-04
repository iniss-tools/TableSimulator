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

    /// <summary>Novy obsah tabule tejto rodiny.</summary>
    BoardDisplay CreateDisplay();
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
    SimBoard? FindBoard(int address);

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
    void Reply(SimBoard board, byte[] bytes, FrameInfo info);
}
