using TableSimulator.Core.Boards;
using ToolsCore.Iniss.Tools;

namespace TableSimulator.Core.Monitor;

/// <summary>
/// Predloha tabule - typ panelu z programu Monitor prevedeny na profil simulatora.
/// </summary>
/// <param name="File">subor typu panelu (.cfg)</param>
/// <param name="Name">nazov typu panelu</param>
/// <param name="Profile">profil tabule</param>
public sealed record BoardTemplate(string File, string Name, BoardProfile Profile);

/// <summary>
/// Instalacia programu ELEN Monitor (<c>Sim\*.cfg</c>, <c>Sim\CONFIG</c>, <c>Sim\FONT</c>, <c>Projects\*.prj</c>, <c>Monitor.INI</c>)
/// ako zdroj predloh tabul a projektov.
/// </summary>
public sealed class MonitorFolder(string root)
{
    /// <summary>Hodiny v hlavicke su 45 bodov od praveho okraja, 4 body zhora, 8 bodov vysoke (ako v Monitore).</summary>
    private const int ClockFromRight = 45;

    private const int ClockTop = 4;
    private const int ClockHeight = 8;

    /// <summary>Priecinok Monitora.</summary>
    public string Root { get; } = root;

    /// <summary>Priecinok typov panelov.</summary>
    public string SimFolder => Path.Combine(Root, "Sim");

    /// <summary>Priecinok pisiem ELEN.</summary>
    public string FontFolder => Path.Combine(SimFolder, "FONT");

    /// <summary>Ci je to instalacia Monitora.</summary>
    public bool Exists => !string.IsNullOrWhiteSpace(Root) && Directory.Exists(SimFolder);

    /// <summary>Vsetky typy panelov ako predlohy, podla nazvu.</summary>
    public IReadOnlyList<BoardTemplate> Panels()
    {
        if (!Exists)
            return [];
        return Directory.EnumerateFiles(SimFolder, "*.cfg")
            .Select(Panel)
            .OfType<BoardTemplate>()
            .OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Predloha z typu panelu; null = subor chyba alebo nie je typ panelu.</summary>
    public BoardTemplate? Panel(string cfgFile)
    {
        var path = Path.IsPathRooted(cfgFile) ? cfgFile : Path.Combine(SimFolder, cfgFile);
        if (!File.Exists(path))
            return null;
        try
        {
            var panel = MonitorPanel.Load(path);
            var config = MonitorPanelConfig.Load(Path.Combine(SimFolder, "CONFIG", panel.ConfigFile));
            return new BoardTemplate(Path.GetFileName(path), panel.Name, ToProfile(panel, config));
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>Mena projektov (bez pripony).</summary>
    public IReadOnlyList<string> Projects()
    {
        var folder = Path.Combine(Root, "Projects");
        if (!Directory.Exists(folder))
            return [];
        return Directory.EnumerateFiles(folder, "*.prj").Select(Path.GetFileNameWithoutExtension).OfType<string>()
            .Order(StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>Simulatory projektu; prazdny zoznam = projekt neexistuje.</summary>
    public IReadOnlyList<MonitorProjectEntry> Project(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return [];
        var path = Path.Combine(Root, "Projects", name + ".prj");
        return File.Exists(path) ? MonitorProject.Load(path) : [];
    }

    /// <summary>Farby a pismo hlavicky z Monitor.INI.</summary>
    public MonitorLook Look()
    {
        var path = Path.Combine(Root, "Monitor.INI");
        return File.Exists(path) ? MonitorLook.Parse(File.ReadAllText(path, Encodings.Win1250)) : MonitorLook.Default;
    }

    /// <summary>
    /// Profil tabule z typu panelu a jeho konfiguracie: rozmer a farebnost z konfiguracie, casy blikania (FlashON a FlashOFF
    /// v desatinach sekundy), pisma a hlavicka z .cfg, hodiny podla konfiguracie.
    /// </summary>
    public static BoardProfile ToProfile(MonitorPanel panel, MonitorPanelConfig? config)
    {
        ArgumentNullException.ThrowIfNull(panel);
        var defaults = new BoardProfile();
        var width = config?.Width ?? (panel.Columns > 0 ? panel.Columns : defaults.Width);
        var headerWidth = panel.Columns > 0 ? panel.Columns : width;

        BoardHeader? header = null;
        if (panel.HeaderHeight > 0)
        {
            header = new BoardHeader
            {
                Height = panel.HeaderHeight,
                Texts = panel.HeaderTexts.Select(t => new HeaderText(t.X, t.Y, t.Height, t.Text)).ToList(),
                Clock = config?.Clock == true ? new HeaderClock(Math.Max(0, headerWidth - ClockFromRight), ClockTop, ClockHeight) : null
            };
        }

        return defaults with
        {
            Width = width,
            Rows = config?.Rows ?? defaults.Rows,
            RowHeight = config?.RowHeight ?? defaults.RowHeight,
            FontFile = string.IsNullOrWhiteSpace(panel.FontFile) ? null : panel.FontFile,
            BlinkOnMs = config is { FlashOn: > 0 } ? config.FlashOn * 100 : defaults.BlinkOnMs,
            BlinkOffMs = config is { FlashOff: > 0 } ? config.FlashOff * 100 : defaults.BlinkOffMs,
            Monochrome = config?.Color == 0,
            SwapRedGreen = config?.Color == 2,
            Header = header
        };
    }
}
