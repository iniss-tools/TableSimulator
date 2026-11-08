namespace TableSimulator.Core.Monitor;

/// <summary>
/// Simulator v projekte Monitora (<c>Projects\*.prj</c>).
/// </summary>
/// <param name="Name">nazov typu panelu</param>
/// <param name="Location">umiestnenie (popis obsluhy)</param>
/// <param name="Addresses">adresy na linke - pri dvojitej adrese dve, kazda pre polovicu riadkov</param>
/// <param name="Monitor">len pocuva (rezim Monitor), inak odpoveda (Displej)</param>
/// <param name="Open">otvorit po spusteni</param>
/// <param name="ConfigFile">subor typu panelu (.cfg)</param>
public sealed record MonitorProjectEntry(string Name, string Location, IReadOnlyList<int> Addresses, bool Monitor, bool Open, string ConfigFile);
