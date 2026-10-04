using System.Globalization;
using ToolsCore.Iniss.Registry;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Monitor;

/// <summary>
/// Konfiguracia panelu ELEN pre program Monitor (<c>Sim\CONFIG\rwp_*.bin</c>, 512 B) - to, co sa do panelu nahra pri
/// vyrobe: rozmer matice, farebnost, hodiny a casy blikania.
/// </summary>
/// <param name="Type">typ panelu (bajt 0)</param>
/// <param name="Rows">pocet riadkov</param>
/// <param name="RowHeight">vyska riadku v bodoch</param>
/// <param name="Width">sirka v bodoch</param>
/// <param name="Color">farebnost (bajt 9): 0 jednofarebny, 1 cervena a zelena, 2 cervena a zelena zamenene</param>
/// <param name="Clock">panel ma hodiny v hlavicke (bajt 27 nenulovy)</param>
/// <param name="FlashOn">ako dlho blikajuci text svieti (bajt 25, jednotky podla Monitora)</param>
/// <param name="FlashOff">ako dlho blikajuci text nesvieti (bajt 26)</param>
/// <param name="Raw">cely obsah suboru</param>
public sealed record MonitorPanelConfig(int Type, int Rows, int RowHeight, int Width, int Color, bool Clock, int FlashOn, int FlashOff, byte[] Raw)
{
    /// <summary>Dlzka suboru.</summary>
    public const int Size = 512;

    /// <summary>Precita konfiguraciu; null = subor nema tvar konfiguracie panelu.</summary>
    public static MonitorPanelConfig? Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != Size || data[1] == 0 || data[2] == 0)
            return null;
        var width = data[3] << 8 | data[4];
        if (width == 0)
            return null;
        return new MonitorPanelConfig(data[0], Math.Min(16, (int)data[1]), Math.Min(16, (int)data[2]), Math.Min(512, width), data[9], data[27] != 0, data[25], data[26], data);
    }

    /// <summary>Nacita konfiguraciu zo suboru; null = chyba alebo zly tvar.</summary>
    public static MonitorPanelConfig? Load(string path)
    {
        try
        {
            return File.Exists(path) ? Read(File.ReadAllBytes(path)) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}

/// <summary>
/// Text hlavicky tabule. Polohy a vyska su v bodoch tabule.
/// </summary>
/// <param name="X">vodorovna poloha laveho okraja</param>
/// <param name="Y">zvisla poloha horneho okraja od vrchu hlavicky</param>
/// <param name="Height">vyska pisma</param>
/// <param name="Text">text</param>
public sealed record MonitorHeaderText(int X, int Y, int Height, string Text);

/// <summary>
/// Typ panelu v programe Monitor (<c>Sim\*.cfg</c>): rozmer, pisma, konfiguracia a hlavicka.
/// </summary>
/// <param name="File">meno suboru .cfg</param>
/// <param name="Name">nazov typu panelu</param>
/// <param name="FontFile">subor pisiem ELEN</param>
/// <param name="ConfigFile">subor konfiguracie panelu (<c>rwp_*.bin</c>)</param>
/// <param name="Zoom">zvacsenie v Monitore - pixelov na bod</param>
/// <param name="Columns">sirka v bodoch</param>
/// <param name="HeaderHeight">vyska hlavicky v bodoch; 0 = bez hlavicky</param>
/// <param name="HeaderTexts">zobrazene texty hlavicky (prvych <c>SimText</c>)</param>
public sealed record MonitorPanel(
    string File,
    string Name,
    string FontFile,
    string ConfigFile,
    int Zoom,
    int Columns,
    int HeaderHeight,
    IReadOnlyList<MonitorHeaderText> HeaderTexts)
{
    /// <summary>Precita typ panelu z textu .cfg.</summary>
    public static MonitorPanel Parse(string file, string text)
    {
        var ini = InissIniFile.Parse(text);
        var count = Number(ini, "HEAD", "SimText");
        var texts = new List<MonitorHeaderText>();
        for (var i = 0; i < count; i++)
        {
            var key = i.ToString(CultureInfo.InvariantCulture);
            if (ini.GetString("HEAD", "txt" + key) is not { } t)
                break;
            texts.Add(new MonitorHeaderText(Number(ini, "HEAD", "x" + key), Number(ini, "HEAD", "y" + key), Number(ini, "HEAD", "h" + key), t));
        }

        return new MonitorPanel(
            file,
            ini.GetString("SIM", "Name") ?? Path.GetFileNameWithoutExtension(file),
            ini.GetString("SIM", "Font") ?? "",
            ini.GetString("SIM", "Config") ?? "",
            Math.Max(1, Number(ini, "SIM", "Velkost")),
            Number(ini, "SIM", "Stlpce"),
            Number(ini, "HEAD", "Vyska"),
            texts);
    }

    /// <summary>Nacita typ panelu zo suboru .cfg (CP1250).</summary>
    public static MonitorPanel Load(string path) => Parse(Path.GetFileName(path), System.IO.File.ReadAllText(path, Encodings.Win1250));

    private static int Number(InissIniFile ini, string section, string name) =>
        int.TryParse(ini.GetString(section, name)?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
}

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

/// <summary>
/// Projekt Monitora: zoznam simulatorov po sest riadkov (nazov, umiestnenie, adresa, display/monitor, yes/no, subor .cfg).
/// </summary>
public static class MonitorProject
{
    /// <summary>Precita projekt; neuplne zaznamy na konci sa vynechaju.</summary>
    public static IReadOnlyList<MonitorProjectEntry> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.Replace("\r", "", StringComparison.Ordinal).Split('\n');
        var entries = new List<MonitorProjectEntry>();
        for (var i = 0; i + 5 < lines.Length; i += 6)
        {
            var addresses = lines[i + 2].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(a => int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : -1)
                .Where(a => a is > 0 and < 128)
                .ToList();
            if (addresses.Count == 0)
                continue;
            entries.Add(new MonitorProjectEntry(lines[i].Trim(), lines[i + 1].Trim(), addresses,
                lines[i + 3].Trim().StartsWith('m'), lines[i + 4].Trim().StartsWith('y'), lines[i + 5].Trim()));
        }

        return entries;
    }

    /// <summary>Nacita projekt zo suboru (CP1250).</summary>
    public static IReadOnlyList<MonitorProjectEntry> Load(string path) => Parse(File.ReadAllText(path, Encodings.Win1250));
}

/// <summary>
/// Vzhlad simulatorov v Monitore (<c>Monitor.INI</c>, sekcia <c>[SIM]</c>) - farby su v poradi Windows (BGR).
/// </summary>
/// <param name="FontName">pismo hlavicky</param>
/// <param name="FontColor">farba pisma hlavicky (#rrggbb)</param>
/// <param name="HeaderColor">pozadie hlavicky (#rrggbb)</param>
/// <param name="DisplayColor">vypln medzi riadkami (#rrggbb)</param>
public sealed record MonitorLook(string FontName, string FontColor, string HeaderColor, string DisplayColor)
{
    /// <summary>Predvolene farby Monitora.</summary>
    public static MonitorLook Default { get; } = new("Arial", "#ffffff", "#000080", "#414141");

    /// <summary>Precita vzhlad z Monitor.INI; chybajuce hodnoty = predvolene.</summary>
    public static MonitorLook Parse(string text)
    {
        var ini = InissIniFile.Parse(text);
        return new MonitorLook(
            ini.GetString("SIM", "FontName") ?? Default.FontName,
            Color(ini.GetString("SIM", "FontColor")) ?? Default.FontColor,
            Color(ini.GetString("SIM", "HeaderColor")) ?? Default.HeaderColor,
            Color(ini.GetString("SIM", "DisplayColor")) ?? Default.DisplayColor);
    }

    /// <summary>Farba Windows (0x00BBGGRR ako cislo) na #rrggbb.</summary>
    public static string? Color(string? value)
    {
        if (!int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bgr) || bgr < 0)
            return null;
        return string.Create(CultureInfo.InvariantCulture, $"#{bgr & 0xFF:x2}{(bgr >> 8) & 0xFF:x2}{(bgr >> 16) & 0xFF:x2}");
    }
}
