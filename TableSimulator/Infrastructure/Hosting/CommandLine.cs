using System.Globalization;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Parametre servera: <c>--data &lt;priecinok&gt;</c> (nastavenia), <c>--config &lt;subor&gt;</c> (linky a tabule),
/// <c>--port &lt;cislo&gt;</c>, <c>--no-browser</c>.
/// </summary>
/// <param name="Paths">priecinky a subory servera</param>
/// <param name="Port">port weboveho rozhrania z prikazoveho riadka; null = z nastavenia</param>
/// <param name="NoBrowser">po spusteni neotvarat prehliadac</param>
public sealed record CommandLine(AppPaths Paths, int? Port, bool NoBrowser)
{
    public static CommandLine Parse(string[] args, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        var data = Arg(args, "--data") is { } folder ? Path.GetFullPath(folder) : AppPaths.DefaultDataFolder;
        var config = Arg(args, "--config") is { } file ? Path.GetFullPath(file) : Path.Combine(data, "config.json");
        int? port = int.TryParse(Arg(args, "--port"), NumberStyles.None, CultureInfo.InvariantCulture, out var p) ? p : null;
        return new CommandLine(new AppPaths(data, config, Path.Combine(baseDirectory, "wwwroot")), port, args.Contains("--no-browser"));
    }

    private static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
