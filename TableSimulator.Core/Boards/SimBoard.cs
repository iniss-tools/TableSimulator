using TableSimulator.Core.Boards.AkSignal;

namespace TableSimulator.Core.Boards;

/// <summary>
/// Simulovana tabula na linke.
/// </summary>
public sealed class SimBoard
{
    public SimBoard(int line, int address, string name, string manufacturerKey, BoardProfile profile, BoardDisplay display)
    {
        Line = line;
        Address = address;
        Name = name;
        ManufacturerKey = manufacturerKey;
        Profile = profile;
        Display = display;
    }

    /// <summary>Identifikator tabule: <c>linka-adresa</c>.</summary>
    public string Id => MakeId(Line, Address);

    /// <summary>Cislo linky.</summary>
    public int Line { get; }

    /// <summary>Adresa tabule na linke.</summary>
    public int Address { get; }

    /// <summary>Druha adresa panelu (dvojita adresa) - ovlada hornu polovicu riadkov; null = len jedna adresa.</summary>
    public int? SecondAddress { get; set; }

    /// <summary>Nazov pre obsluhu.</summary>
    public string Name { get; set; }

    /// <summary>Kluc vyrobcu (<c>MANUFACTURER_KEY</c>), urcuje variant protokolu.</summary>
    public string ManufacturerKey { get; set; }

    /// <summary>Profil - hardver tabule.</summary>
    public BoardProfile Profile { get; set; }

    /// <summary>Co scenar docasne nastavuje ustredni AK Signal nad jej profil; null = nic.</summary>
    public AkSignalOverride? Override { get; internal set; }

    /// <summary>Profil, podla ktoreho tabula odpoveda - <see cref="Profile" /> s prekrytim zo scenara.</summary>
    public BoardProfile EffectiveProfile => Override is { } o
        ? Profile with { AkSignal = o.Apply(Profile.AkSignal ?? AkSignalProfile.Default) }
        : Profile;

    /// <summary>Identifikator vzhladu; null = predvoleny pre technologiu.</summary>
    public string? Design { get; set; }

    /// <summary>Predstierana porucha.</summary>
    public BoardFault Fault { get; set; } = BoardFault.None;

    /// <summary>Spravy od INISSu patria inemu vyrobcovi; null = nic take sa nezistilo.</summary>
    public ManufacturerMismatch? Mismatch { get; internal set; }

    /// <summary>Predstierany vypadok napajania: tabula nic neprijima ani neodpoveda, po navrate je prazdna.</summary>
    public bool PoweredOff { get; internal set; }

    /// <summary>Tabulu vytvorilo samoucenie (prva sprava na neznamu adresu).</summary>
    public bool AutoCreated { get; init; }

    /// <summary>Obsah tabule.</summary>
    public BoardDisplay Display { get; }

    /// <summary>Pocitadla komunikacie (pri zmene nastavenia tabule ostanu).</summary>
    public BoardStats Stats { get; init; } = new();

    /// <summary>Zvysi sa pri kazdej zmene obsahu alebo nastavenia.</summary>
    public long Version { get; internal set; }

    /// <summary>Identifikator tabule z linky a adresy.</summary>
    public static string MakeId(int line, int address) => $"{line}-{address}";
}
