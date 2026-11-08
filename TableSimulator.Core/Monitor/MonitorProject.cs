using System.Globalization;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Monitor;

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
