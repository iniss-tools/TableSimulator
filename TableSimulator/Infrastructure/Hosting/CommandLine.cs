using System.Globalization;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Parametre servera: <c>--data &lt;priecinok&gt;</c> (nastavenia a databaza), <c>--port &lt;cislo&gt;</c>,
/// <c>--no-browser</c>, <c>--reset-admin [meno]</c> (nove heslo spravcu a koniec bez spustenia servera),
/// <c>--install-service</c> a <c>--uninstall-service</c> (sluzba Windows s tymito <c>--data</c> a <c>--port</c>). Ak parameter
/// chyba, plati premenna prostredia <c>TABLESIMULATOR_DATA</c>, <c>TABLESIMULATOR_NO_BROWSER=1</c> (testy servera); sluzba
/// prehliadac neotvara a bez priecinka nastaveni pouzije <see cref="WindowsService.DefaultDataFolder" />.
/// Zruseny <c>--config</c> (linky a tabule su v databaze) sa len ohlasi.
/// </summary>
/// <param name="Paths">priecinky a subory servera</param>
/// <param name="Port">port weboveho rozhrania z prikazoveho riadka; null = z nastavenia</param>
/// <param name="NoBrowser">po spusteni neotvarat prehliadac</param>
/// <param name="ResetAdmin">len obnovit pristup spravcu (<c>--reset-admin</c>) a skoncit</param>
/// <param name="ResetAdminName">meno spravcu pri <c>--reset-admin</c>; null = jediny spravca</param>
/// <param name="ObsoleteConfig">zadany zruseny parameter <c>--config</c></param>
/// <param name="InstallService">nainstalovat sluzbu Windows a skoncit</param>
/// <param name="UninstallService">odstranit sluzbu Windows a skoncit</param>
public sealed record CommandLine(AppPaths Paths, int? Port, bool NoBrowser, bool ResetAdmin = false, string? ResetAdminName = null,
    bool ObsoleteConfig = false, bool InstallService = false, bool UninstallService = false)
{
    private const string ResetAdminOption = "--reset-admin";

    /// <param name="args">parametre programu</param>
    /// <param name="baseDirectory">priecinok programu (wwwroot)</param>
    /// <param name="service">program bezi ako sluzba Windows</param>
    public static CommandLine Parse(string[] args, string baseDirectory, bool service = false)
    {
        ArgumentNullException.ThrowIfNull(args);
        var data = (Arg(args, "--data") ?? Variable("TABLESIMULATOR_DATA")) is { } folder
            ? Path.GetFullPath(folder)
            : service ? WindowsService.DefaultDataFolder : AppPaths.DefaultDataFolder;
        int? port = int.TryParse(Arg(args, "--port"), NumberStyles.None, CultureInfo.InvariantCulture, out var p) ? p : null;
        var noBrowser = service || args.Contains("--no-browser") || Variable("TABLESIMULATOR_NO_BROWSER") == "1";
        var reset = Array.IndexOf(args, ResetAdminOption);
        var resetName = reset >= 0 && reset + 1 < args.Length && !args[reset + 1].StartsWith("--", StringComparison.Ordinal) ? args[reset + 1] : null;
        return new CommandLine(new AppPaths(data, Path.Combine(baseDirectory, "wwwroot")), port, noBrowser, reset >= 0, resetName,
            args.Contains("--config"), args.Contains("--install-service"), args.Contains("--uninstall-service"));
    }

    private static string? Variable(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    private static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
