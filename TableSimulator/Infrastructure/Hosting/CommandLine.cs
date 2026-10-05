using System.Globalization;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Parametre servera: <c>--data &lt;priecinok&gt;</c> (nastavenia), <c>--config &lt;subor&gt;</c> (linky a tabule),
/// <c>--port &lt;cislo&gt;</c>, <c>--no-browser</c>. Ak parameter chyba, plati premenna prostredia
/// <c>TABLESIMULATOR_DATA</c>, <c>TABLESIMULATOR_CONFIG</c>, <c>TABLESIMULATOR_NO_BROWSER=1</c> (testy servera).
/// </summary>
/// <param name="Paths">priecinky a subory servera</param>
/// <param name="Port">port weboveho rozhrania z prikazoveho riadka; null = z nastavenia</param>
/// <param name="NoBrowser">po spusteni neotvarat prehliadac</param>
public sealed record CommandLine(AppPaths Paths, int? Port, bool NoBrowser)
{
    public static CommandLine Parse(string[] args, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        var data = (Arg(args, "--data") ?? Variable("TABLESIMULATOR_DATA")) is { } folder ? Path.GetFullPath(folder) : AppPaths.DefaultDataFolder;
        var config = (Arg(args, "--config") ?? Variable("TABLESIMULATOR_CONFIG")) is { } file ? Path.GetFullPath(file) : Path.Combine(data, "config.json");
        int? port = int.TryParse(Arg(args, "--port"), NumberStyles.None, CultureInfo.InvariantCulture, out var p) ? p : null;
        var noBrowser = args.Contains("--no-browser") || Variable("TABLESIMULATOR_NO_BROWSER") == "1";
        return new CommandLine(new AppPaths(data, config, Path.Combine(baseDirectory, "wwwroot")), port, noBrowser);
    }

    private static string? Variable(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    private static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
