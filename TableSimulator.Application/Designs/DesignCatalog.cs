using System.Text.Json;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Application.Designs;

/// <summary>
/// Vzhlady tabul: vstavane vo <c>wwwroot/designs</c> a vlastne v priecinku vzhladov (rovnake id vstavany prekryje).
/// Kazdy vzhlad je priecinok s <c>design.json</c>.
/// </summary>
public sealed class DesignCatalog(AppPaths paths, ISettingsStore settings)
{
    /// <summary>Vsetky vzhlady podla nazvu.</summary>
    public DesignInfoDto[] List()
    {
        var result = new Dictionary<string, DesignInfoDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var (dir, builtIn) in new[] { (paths.BuiltInDesignsFolder, true), (paths.DesignsFolder(settings.Current), false) })
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                continue;
            foreach (var designDir in Directory.EnumerateDirectories(dir))
            {
                if (Read(designDir, builtIn) is { } info)
                    result[info.Id] = !builtIn && result.TryGetValue(info.Id, out var original) && original.BuiltIn ? info with { Overrides = true } : info;
            }
        }

        return result.Values.OrderBy(d => d.Name, StringComparer.CurrentCulture).ToArray();
    }

    private static DesignInfoDto? Read(string dir, bool builtIn)
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
            return new DesignInfoDto(id, name, renderer, builtIn);
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return null;
        }
    }
}
