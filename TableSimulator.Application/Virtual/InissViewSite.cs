using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using TableSimulator.Application.Settings;
using TableSimulator.Contracts.Virtual;
using TableSimulator.Core.Virtual;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Application.Virtual;

/// <summary>
/// Instalacia INISSView z nastavenia (<see cref="AppSettings.InissViewFolder" />): sablony XSL, ich CSS a obrazky,
/// skript <c>JScript.js</c> a nastavenie skriptu z <c>Web.config</c>. Virtualna tabula so sablonou sa ukaze ako
/// stranka <c>Tabule.aspx</c> INISSView - relativne adresy stranky smeruju na <see cref="BasePath" />.
/// </summary>
public sealed partial class InissViewSite(ISettingsStore settings, XslTablePages pages)
{
    /// <summary>Kam smeruju relativne adresy stranky (koren aplikacie INISSView).</summary>
    public const string BasePath = "/inissview/app/";

    /// <summary>Subory, ktore server z priecinka INISSView posiela - len vzhlad a skripty stranok, nie nastavenie ani program.</summary>
    private static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".gif", ".png", ".jpg", ".jpeg", ".bmp", ".ico", ".svg", ".woff", ".woff2", ".ttf"
    };

    private readonly Lock _lock = new();
    private (string Path, DateTime Modified, InissViewConfig? Config) _config = ("", DateTime.MinValue, null);

    /// <summary>Priecinok aplikacie INISSView; prazdny = nenastaveny.</summary>
    public string Folder => settings.Current.InissViewFolder;

    /// <summary>Sablony sa pouziju - priecinok je nastaveny.</summary>
    public bool Enabled => Folder.Length > 0;

    /// <summary>Priecinok sablon: <c>TabXslDir</c> z Web.config, ak na tomto pocitaci existuje, inak <c>tabs</c> v aplikacii.</summary>
    public string TemplatesFolder => Config()?.XslFolder is { Length: > 0 } folder && Directory.Exists(folder) ? folder : Path.Combine(Folder, "tabs");

    /// <summary>Mena sablon (bez pripony) na vyber.</summary>
    public IReadOnlyList<string> Templates()
    {
        if (!Enabled || !Directory.Exists(TemplatesFolder))
            return [];
        return [.. Directory.EnumerateFiles(TemplatesFolder, "*.xsl").Select(Path.GetFileNameWithoutExtension).OfType<string>().Order(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Stranka tabule zo sablony; pri chybe jej kod a podrobnost.</summary>
    public VirtualPageDto Render(VirtualBoard board, XmlTableSource? source)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (source is null)
            return Problem("noSource", board.Source);
        var name = board.Template.Trim();
        if (name.IndexOfAny(['/', '\\', ':']) >= 0)
            return Problem("notFound", name);
        if (Path.GetExtension(name).Length == 0)
            name += ".xsl";
        var path = Path.Combine(TemplatesFolder, name);
        if (!File.Exists(path))
            return Problem("notFound", path);

        try
        {
            var options = new XslTableOptions(board.RowCount, board.RowStart, board.Separator, board.Parameter);
            return new VirtualPageDto(Prepare(pages.Render(source.Path, path, options, Config()?.Script ?? [])), null, null);
        }
        catch (XsltException e)
        {
            return Problem("template", e.Message);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return Problem("failed", e.Message);
        }
    }

    /// <summary>Subor stranky (CSS, obrazok, skript) v priecinku INISSView; null = nie je alebo sa neposiela.</summary>
    /// <param name="relative">cesta od korena aplikacie (<c>tabs/odchody.css</c>)</param>
    public string? Asset(string relative)
    {
        ArgumentNullException.ThrowIfNull(relative);
        if (!Enabled || !AssetExtensions.Contains(Path.GetExtension(relative)))
            return null;
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Folder)) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full) ? full : null;
    }

    private static VirtualPageDto Problem(string code, string detail) => new(null, code, detail);

    [GeneratedRegex("""<meta\s[^>]*http-equiv\s*=\s*["']?refresh["']?[^>]*>""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MetaRefresh();

    /// <summary>
    /// Stranka do ramu: bez obnovy znacku <c>meta refresh</c> (stranku po zmene XML obnovuje simulator) a s prvkom
    /// <c>base</c> hned za zaciatkom hlavicky (inak na zaciatku) - relativne adresy do priecinka INISSView.
    /// </summary>
    private static string Prepare(string html)
    {
        const string tag = "<base href=\"" + BasePath + "\">";
        html = MetaRefresh().Replace(html, "");
        var head = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
        var end = head >= 0 ? html.IndexOf('>', head) : -1;
        return end >= 0 ? html.Insert(end + 1, tag) : tag + html;
    }

    /// <summary>Web.config INISSView (podla casu zmeny); null = nie je alebo je chybny.</summary>
    private InissViewConfig? Config()
    {
        if (!Enabled)
            return null;
        var path = Path.Combine(Folder, "Web.config");
        var modified = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        lock (_lock)
        {
            if (_config.Path == path && _config.Modified == modified)
                return _config.Config;
        }

        InissViewConfig? config = null;
        if (modified != DateTime.MinValue)
        {
            try
            {
                // kodovanie podla deklaracie (UTF-8 aj windows-1250 - kodove stranky registruje ToolsCore.Iniss)
                _ = Encodings.Win1250;
                config = InissViewConfig.Parse(XDocument.Load(path).ToString());
            }
            catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
            {
                // bez nastavenia skriptu a priecinka sablon - stranky aj tak idu
            }
        }

        lock (_lock)
            _config = (path, modified, config);
        return config;
    }
}
