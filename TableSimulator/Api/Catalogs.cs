using System.Collections.Concurrent;
using System.Text.Json;
using ToolsCore.Iniss.Elen;

namespace TableSimulator.Api;

/// <summary>Pismo ELEN pre prehliadac: sirky znakov a bitove mapy v base64.</summary>
public sealed record FontDto(string Name, int MaxWidth, int Height, bool Proportional, int BytesPerRow, int[] Widths, string[] Glyphs);

/// <summary>Subor pisiem ELEN pre prehliadac.</summary>
public sealed record FontFileDto(string File, string Name, FontDto[] Fonts);

/// <summary>
/// Subory pisiem ELEN z priecinka pisiem - prehliadac podla nich kresli presne body tabule.
/// </summary>
public sealed class FontCatalog(Func<string> folder)
{
    private readonly ConcurrentDictionary<string, (DateTime Stamp, FontFileDto Font)> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Mena suborov .bin v priecinku pisiem, ktore sa daju nacitat ako pisma.</summary>
    public string[] List()
    {
        var dir = folder();
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return [];
        return Directory.EnumerateFiles(dir, "*.bin").Select(Path.GetFileName).OfType<string>()
            .Where(f => Get(f) != null).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Subor pisiem podla mena; null = neexistuje alebo nie je pismo.</summary>
    public FontFileDto? Get(string file)
    {
        var dir = folder();
        if (string.IsNullOrWhiteSpace(dir) || file.IndexOfAny(['/', '\\', ':']) >= 0 || !file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = Path.Combine(dir, file);
        if (!File.Exists(path))
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
}

/// <summary>Vzhlad v zozname.</summary>
public sealed record DesignInfo(string Id, string Name, string Renderer, bool BuiltIn);

/// <summary>
/// Vzhlady tabul: vstavane vo <c>wwwroot/designs</c> a vlastne v priecinku vzhladov (rovnake id vstavany prekryje).
/// Kazdy vzhlad je priecinok s <c>design.json</c>.
/// </summary>
public sealed class DesignCatalog(string builtInFolder, Func<string> userFolder)
{
    /// <summary>Vsetky vzhlady.</summary>
    public DesignInfo[] List()
    {
        var result = new Dictionary<string, DesignInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var (dir, builtIn) in new[] { (builtInFolder, true), (userFolder(), false) })
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                continue;
            foreach (var designDir in Directory.EnumerateDirectories(dir))
            {
                var info = Read(designDir, builtIn);
                if (info != null)
                    result[info.Id] = info;
            }
        }

        return result.Values.OrderBy(d => d.Name, StringComparer.CurrentCulture).ToArray();
    }

    private static DesignInfo? Read(string dir, bool builtIn)
    {
        var file = Path.Combine(dir, "design.json");
        if (!File.Exists(file))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var root = doc.RootElement;
            var id = Path.GetFileName(dir);
            var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
            var renderer = root.TryGetProperty("renderer", out var r) ? r.GetString() ?? "" : "";
            return new DesignInfo(id, name, renderer, builtIn);
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return null;
        }
    }
}
