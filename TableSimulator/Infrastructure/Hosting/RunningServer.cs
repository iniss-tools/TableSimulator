using System.Diagnostics;
using System.ServiceProcess;
using Serilog;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Spustenie programu nad priecinkom nastaveni, nad ktorym uz server bezi (odkaz v ponuke Start, ked bezi sluzba,
/// alebo druhe spustenie v okne): druhy server sa nespusti, otvori sa adresa bieziaceho.
/// </summary>
public static class RunningServer
{
    /// <summary>Ako dlho sa caka, kym bieziaci alebo prave spustany server zapise adresu.</summary>
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(60);

    /// <summary>Priecinok nastaveni patri nainstalovanej sluzbe Windows a tento proces nie je ona.</summary>
    public static bool BelongsToService(string dataFolder) =>
        !WindowsService.IsRunning && WindowsService.InstalledDataFolder() is { } serviceFolder && SamePath(serviceFolder, dataFolder);

    /// <summary>Otvori bieziaci server v prehliadaci; vrati kod ukoncenia programu.</summary>
    /// <param name="dataFolder">priecinok nastaveni</param>
    /// <param name="noBrowser">adresu len vypisat</param>
    /// <param name="service">priecinok patri sluzbe - ked nebezi, nema zmysel cakat</param>
    public static async Task<int> OpenAsync(string dataFolder, bool noBrowser, bool service)
    {
        if (service && !WindowsService.IsStartingOrRunning())
        {
            Log.Error("Sluzba {Service} s nastaveniami {DataFolder} nebezi. Spusta sa sama so systemom (s oneskorenim), hned ju spusti " +
                "sc start {Service} v prikazovom riadku spustenom ako spravca alebo okno Sluzby.", WindowsService.Name, dataFolder, WindowsService.Name);
            ConsoleWindow.PauseIfOwned(Console.Out);
            return 1;
        }

        if (await ServerLock.ReadUrlAsync(dataFolder, Wait).ConfigureAwait(false) is not { } url)
        {
            Log.Error("Server s nastaveniami {DataFolder} sa nespustil ani po {Seconds} s - pozrite jeho log.", dataFolder, (int)Wait.TotalSeconds);
            ConsoleWindow.PauseIfOwned(Console.Out);
            return 1;
        }

        Log.Information("TableSimulator uz bezi: {Url} (nastavenia: {DataFolder})", url, dataFolder);
        if (!noBrowser)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        return 0;
    }

    private static bool SamePath(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
            StringComparison.OrdinalIgnoreCase);
}
