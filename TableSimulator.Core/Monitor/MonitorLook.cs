using System.Globalization;
using ToolsCore.Iniss.Registry;

namespace TableSimulator.Core.Monitor;

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
