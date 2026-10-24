using System.Diagnostics;
using Serilog;
using Serilog.Core;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Spustenie programu nad priecinkom nastaveni, nad ktorym uz server bezi (odkaz v ponuke Start, ked bezi sluzba,
/// alebo druhe spustenie v okne): druhy server sa nespusti, otvori sa adresa bieziaceho. Zastavenu sluzbu odkaz spusti.
/// </summary>
public static class RunningServer
{
    /// <summary>Ako dlho sa caka, kym bieziaci alebo prave spustany server zapise adresu.</summary>
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(60);

    /// <summary>Priecinok nastaveni patri nainstalovanej sluzbe Windows a tento proces nie je ona.</summary>
    public static bool BelongsToService(string dataFolder) =>
        !WindowsService.IsRunning && WindowsService.InstalledDataFolder() is { } serviceFolder && SamePath(serviceFolder, dataFolder);

    /// <summary>Otvori bieziaci server v prehliadaci; vrati kod ukoncenia programu.</summary>
    /// <param name="paths">priecinok nastaveni</param>
    /// <param name="noBrowser">adresu len vypisat</param>
    /// <param name="service">priecinok patri sluzbe - zastavenu sluzbu spusti a pri zlyhani ukaze dovod</param>
    public static async Task<int> OpenAsync(AppPaths paths, bool noBrowser, bool service)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var dataFolder = paths.DataFolder;
        string? url;
        if (service)
        {
            var since = DateTime.UtcNow;
            // po zlyhani caka sluzba na dalsi pokus alebo ju Windows uz nespusti - spusti sa hned (pravo ma kazdy prihlaseny pouzivatel)
            if (!WindowsService.IsStartingOrRunning())
            {
                Log.Information("Sluzba {Service} nebezi - spusta sa", WindowsService.Name);
                if (WindowsService.TryStart() is { } error)
                    return Fail("Sluzbu {Service} sa nepodarilo spustit ({Error}). Spustite ju sc start {Service} v prikazovom riadku spustenom ako spravca " +
                                "alebo v okne Sluzby.", WindowsService.Name, error, WindowsService.Name);
            }

            var started = await WindowsService.WaitForServerAsync(dataFolder, since, Wait).ConfigureAwait(false);
            if (started.Url is null)
                return Fail("Server sluzby {Service} sa nespustil: {Reason}", WindowsService.Name,
                    started.Error ?? $"ani po {(int)Wait.TotalSeconds} s nepise adresu - pozrite log v {paths.LogsFolder}");
            url = started.Url;
        }
        else if ((url = await ServerLock.ReadUrlAsync(dataFolder, Wait).ConfigureAwait(false)) is null)
        {
            return Fail("Server s nastaveniami {DataFolder} sa nespustil ani po {Seconds} s - pozrite jeho log.", dataFolder, (int)Wait.TotalSeconds);
        }

        Log.Information("TableSimulator uz bezi: {Url} (nastavenia: {DataFolder})", url, dataFolder);
        if (!noBrowser)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        return 0;
    }

    [MessageTemplateFormatMethod("message")]
    private static int Fail(string message, params object?[] values)
    {
        Log.Error(message, values);
        ConsoleWindow.PauseIfOwned(Console.Out);
        return 1;
    }

    /// <summary>Ta ista cesta k priecinku (bez ohladu na velkost pismen a lomku na konci).</summary>
    internal static bool SamePath(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
            StringComparison.OrdinalIgnoreCase);
}
