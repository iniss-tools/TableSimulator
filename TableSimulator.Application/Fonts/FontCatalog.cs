using System.Collections.Concurrent;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Fonts;
using TableSimulator.Core.Monitor;
using ToolsCore.Iniss.Elen;

namespace TableSimulator.Application.Fonts;

/// <summary>
/// Subory pisiem ELEN - vlastny priecinok pisiem, potom pisma programu ELEN Monitor. Prehliadac podla nich kresli
/// presne body tabule.
/// </summary>
public sealed class FontCatalog(ISettingsStore settings)
{
    private readonly ConcurrentDictionary<string, (DateTime Stamp, FontFileDto Font)> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Meno suboru pisiem bez cesty s priponou .bin.</summary>
    public static bool IsValidFileName(string? file) =>
        !string.IsNullOrWhiteSpace(file) && file.IndexOfAny(['/', '\\', ':']) < 0 && file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase);

    /// <summary>Mena suborov .bin, ktore sa daju nacitat ako pisma.</summary>
    public string[] List() =>
        Folders().SelectMany(dir => Directory.EnumerateFiles(dir, "*.bin")).Select(Path.GetFileName).OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase).Where(f => Get(f) != null).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>Subor pisiem podla mena; null = neexistuje alebo nie je pismo.</summary>
    public FontFileDto? Get(string file)
    {
        if (!IsValidFileName(file))
            return null;

        var path = Folders().Select(dir => Path.Combine(dir, file)).FirstOrDefault(File.Exists);
        if (path == null)
            return null;

        var stamp = File.GetLastWriteTimeUtc(path);
        if (_cache.TryGetValue(path, out var cached) && cached.Stamp == stamp)
            return cached.Font;

        try
        {
            var source = ElenFontFile.Load(path);
            if (source.Fonts.Count == 0)
                return null;
            var dto = new FontFileDto(file, source.Name, source.Fonts.Select(f => new FontDto(
                f.Name, f.MaxWidth, f.Height, f.IsProportional, f.BytesPerRow,
                f.Glyphs.Select(g => g.Width).ToArray(),
                f.Glyphs.Select(g => Convert.ToBase64String(g.Data)).ToArray())).ToArray());
            _cache[path] = (stamp, dto);
            return dto;
        }
        catch (Exception e) when (e is IOException or FormatException or EndOfStreamException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Priecinky pisiem, ktore existuju, v poradi priority.</summary>
    private IEnumerable<string> Folders()
    {
        var current = settings.Current;
        var monitor = new MonitorFolder(current.MonitorFolder);
        return new[] { current.FontsFolder, monitor.FontFolder, monitor.SimFolder }.Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d));
    }
}
