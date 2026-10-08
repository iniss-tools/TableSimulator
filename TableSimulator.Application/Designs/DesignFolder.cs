using System.Text.Encodings.Web;
using System.Text.Json;
using TableSimulator.Application.Settings;

namespace TableSimulator.Application.Designs;

/// <summary>Priecinok vlastnych vzhladov: predvoleny (v priecinku nastaveni) sa zalozi, ak chyba.</summary>
public static class DesignFolder
{
    /// <summary>Zapis <c>design.json</c> - citatelne odsadeny, diakritika bez escapovania.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Priecinok vlastnych vzhladov; null, ak je nastaveny a neexistuje.</summary>
    public static string? Prepare(AppPaths paths, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(settings);
        var folder = paths.DesignsFolder(settings);
        if (string.IsNullOrWhiteSpace(settings.DesignsFolder))
            Directory.CreateDirectory(folder);
        return Directory.Exists(folder) ? folder : null;
    }
}
