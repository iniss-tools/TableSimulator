using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Connections;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Dovod, preco sa server nespustil, v subore priecinka nastaveni - sluzba Windows nema okno, kde by ho pouzivatel
/// videl. Precita ho instalacia sluzby a spustenie z ponuky Start (<see cref="WindowsService.WaitForServerAsync" />);
/// po uspesnom starte sa subor zmaze.
/// </summary>
public static partial class StartupError
{
    /// <summary>Subor v priecinku nastaveni.</summary>
    public const string FileName = "startup-error.txt";

    /// <summary>Dovod pre cloveka - obsadeny port s radou, inak sprava vynimky.</summary>
    public static string Describe(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (IsAddressInUse(error))
        {
            // adresu ma len vonkajsia vynimka Kestrelu: "Failed to bind to address http://127.0.0.1:5470: address already in use."
            var port = PortPattern().Match(error.Message) is { Success: true } match ? match.Groups[1].Value : null;
            return $"Port {port ?? "webu"} uz pouziva iny program - mozno TableSimulator spusteny mimo sluzby (v okne alebo z vyvojoveho prostredia). " +
                   "Zatvorte ho alebo spustite server s inym portom (--port).";
        }

        return error.InnerException is null ? error.Message : $"{error.Message} ({error.GetBaseException().Message})";
    }

    /// <summary>Kestrel neotvoril port, lebo ho uz pouziva iny program (host tuto chybu zapise aj s vynimkou sam).</summary>
    public static bool IsAddressInUse(Exception error)
    {
        for (var e = error; e is not null; e = e.InnerException)
            if (e is AddressInUseException)
                return true;
        return false;
    }

    /// <summary>Zapise dovod neuspesneho startu.</summary>
    public static void Write(string dataFolder, Exception error)
    {
        try
        {
            File.WriteAllText(Path.Combine(dataFolder, FileName), Describe(error));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // dovod je aj v logu
        }
    }

    /// <summary>Server sa spustil - starsi dovod neplati.</summary>
    public static void Clear(string dataFolder)
    {
        try
        {
            File.Delete(Path.Combine(dataFolder, FileName));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // ostane do dalsieho zlyhania - cita sa len novsi nez spustenie
        }
    }

    /// <summary>Dovod zapisany od <paramref name="sinceUtc" />; null, ak ziadny nie je.</summary>
    public static string? ReadSince(string dataFolder, DateTime sinceUtc)
    {
        var file = new FileInfo(Path.Combine(dataFolder, FileName));
        try
        {
            return file.Exists && file.LastWriteTimeUtc >= sinceUtc ? File.ReadAllText(file.FullName).Trim() : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // proces ho prave zapisuje - precita sa v dalsom kole
            return null;
        }
    }

    // port za hostitelom (aj [::1]) v adrese http(s)://…
    [GeneratedRegex(@"://(?:\[[^\]]*\]|[^:/\s]+):(\d+)")]
    private static partial Regex PortPattern();
}
