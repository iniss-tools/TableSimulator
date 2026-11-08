using TableSimulator.Core.Protocols;

namespace TableSimulator.Core.Lines;

/// <summary>
/// Komunikacna linka simulatora - zodpoveda jednej sekcii <c>Driver*</c> v INISSe.
/// </summary>
public sealed class SimLine
{
    internal SimLine(int number, int tableClass, ILineProtocol protocol)
    {
        Number = number;
        TableClass = tableClass;
        Protocol = protocol;
    }

    /// <summary>Cislo linky 1-100 (ako v INISSe - podla neho sa k nej priraduju tabule).</summary>
    public int Number { get; }

    /// <summary>Trieda linky (<c>TableClass</c>) - rodina protokolu.</summary>
    public int TableClass { get; }

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; set; } = "";

    /// <summary>Kde linka pocuva.</summary>
    public LineEndpoint Endpoint { get; set; } = LineEndpoint.DefaultFor(1);

    /// <summary>Rezim - odpovedat alebo len pocuvat.</summary>
    public LineMode Mode { get; set; }

    /// <summary>Na spravu pre neznamu adresu vytvorit tabulu.</summary>
    public bool AutoLearn { get; set; } = true;

    /// <summary>Vyrobca tabul vytvorenych samoucenim.</summary>
    public string Manufacturer { get; set; } = "ELEN";

    /// <summary>Kam sa kopiruju spravy od INISSu (seriovy port, napr. pre ELEN Monitor); null = nikam.</summary>
    public LineEndpoint? Mirror { get; set; }

    /// <summary>Chyba zrkadla (port neexistuje, je obsadeny…); null = v poriadku.</summary>
    public string? MirrorError { get; internal set; }

    /// <summary>Stav spojenia.</summary>
    public LineStatus Status { get; internal set; }

    /// <summary>Podrobnosti stavu - pripojeny klient alebo chyba.</summary>
    public string? StatusDetail { get; internal set; }

    /// <summary>Predstierany vypadok linky - prenos linky nebezi (<see cref="LineStatus.Outage" />).</summary>
    public bool Outage { get; internal set; }

    /// <summary>Od kedy linka je v sucasnom stave.</summary>
    public DateTimeOffset StatusSince { get; internal set; }

    /// <summary>Kedy prisli posledne bajty od INISSu (nie zo zaznamu); null = este nic.</summary>
    public DateTimeOffset? LastReceivedAt { get; internal set; }

    /// <summary>Pocitadla (pri zmene nastavenia linky ostanu).</summary>
    public LineStats Stats { get; init; } = new();

    /// <summary>Zvysi sa pri kazdej zmene.</summary>
    public long Version { get; internal set; }

    internal ILineProtocol Protocol { get; }
}
