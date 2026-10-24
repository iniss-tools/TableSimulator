using System.Security.Cryptography;
using System.Text;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Ktory server odpoveda: hlavicka kazdej odpovede s odtlackom pocitaca a priecinka nastaveni. Na tej istej adrese moze
/// po case bezat iny server (sluzba namiesto programu v okne) - web si pamata prvy odtlacok a pri inom sa nacita znova,
/// aby obsluha nepracovala s udajmi ineho servera v domneni, ze su povodne. Restart toho isteho servera odtlacok nemeni.
/// </summary>
public static class ServerIdentity
{
    /// <summary>Hlavicka odpovede (pozna ju web v <c>src/api/server-identity.ts</c>).</summary>
    public const string Header = "X-TableSimulator-Server";

    /// <summary>Odtlacok pocitaca a priecinka nastaveni (bez cesty v citatelnej podobe).</summary>
    public static string Of(string machine, string dataFolder)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(dataFolder);
        var folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataFolder));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{machine}|{folder}".ToUpperInvariant())))[..16];
    }

    /// <summary>Hlavicka <see cref="Header" /> do kazdej odpovede.</summary>
    public static WebApplication UseServerIdentity(this WebApplication app, AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(paths);
        var identity = Of(Environment.MachineName, paths.DataFolder);
        app.Use((context, next) =>
        {
            context.Response.Headers[Header] = identity;
            return next(context);
        });
        return app;
    }
}
