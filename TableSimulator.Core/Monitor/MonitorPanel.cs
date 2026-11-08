using System.Globalization;
using ToolsCore.Iniss.Registry;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Monitor;

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
