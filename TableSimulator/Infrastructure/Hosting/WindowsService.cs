using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Win32;
using TableSimulator.Infrastructure.Auth;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Ako dopadlo spustenie servera sluzby.</summary>
/// <param name="Url">adresa bieziaceho servera; null = nespustil sa</param>
/// <param name="Error">dovod; null = server sa nespustil v case a dovod nie je znamy</param>
public sealed record ServerStart(string? Url, string? Error);

/// <summary>
/// Server ako sluzba Windows: bezi bez prihlaseneho pouzivatela, startuje so systemom (oneskorene) a po pade alebo
/// restarte z weboveho rozhrania ho Windows spusti znova. Instalacia a odinstalovanie cez <c>sc.exe</c> (prava spravcu).
/// </summary>
public static class WindowsService
{
    /// <summary>Meno sluzby.</summary>
    public const string Name = "TableSimulator";

    /// <summary>Zobrazovane meno sluzby.</summary>
    public const string DisplayName = "Simulator tabul (TableSimulator)";

    /// <summary>Kod ukoncenia pri restarte z weboveho rozhrania - Windows sluzbu spusti znova (akcia po zlyhani).</summary>
    public const int RestartExitCode = 3;

    /// <summary>Kod ukoncenia instalacie: sluzba je nainstalovana, ale server sa nespustil (instalator ukaze dovod).</summary>
    public const int NotStartedExitCode = 4;

    /// <summary>
    /// Prava k sluzbe (SDDL): predvolene Windows, navyse spustenie (RP) pre interaktivnych pouzivatelov - odkaz v ponuke
    /// Start zastavenu sluzbu spusti bez prav spravcu. Zastavit a menit ju smie len spravca.
    /// </summary>
    public const string Security =
        "D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWRPLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)";

    /// <summary>Ako dlho sa caka, kym spustena sluzba otvori web.</summary>
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Ako dlho sa po zastaveni sluzby caka na dovod - proces ho zapise az po hlaseni zastavenia.</summary>
    private static readonly TimeSpan ReasonWait = TimeSpan.FromSeconds(3);

    /// <summary>Win32 ERROR_SERVICE_ALREADY_RUNNING.</summary>
    private const int AlreadyRunning = 1056;

    private const string Description = "Simulator tabul pre INISS - linky k tabuliam, zobrazenie tabul a virtualne tabule na webe.";

    /// <summary>Priecinok nastaveni v prikazovom riadku sluzby.</summary>
    private const string DataOption = "--data \"";

    /// <summary>Proces bezi ako sluzba Windows.</summary>
    public static bool IsRunning => OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();

    /// <summary>Predvoleny priecinok nastaveni sluzby (bez <c>--data</c>) - ucet sluzby nema priecinok pouzivatela.</summary>
    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TableSimulator");

    /// <summary>Prikazovy riadok sluzby (cesta k programu a parametre).</summary>
    public static string BinaryPath(string program, string dataFolder, int? port)
    {
        var path = $"\"{program}\" {DataOption}{Path.TrimEndingDirectorySeparator(dataFolder)}\"";
        return port is { } p ? path + string.Create(CultureInfo.InvariantCulture, $" --port {p}") : path;
    }

    /// <summary>Priecinok nastaveni z prikazoveho riadka sluzby (<see cref="BinaryPath" />); bez <c>--data</c> null.</summary>
    public static string? DataFolderOf(string binaryPath)
    {
        ArgumentNullException.ThrowIfNull(binaryPath);
        var start = binaryPath.IndexOf(DataOption, StringComparison.Ordinal);
        if (start < 0)
            return null;
        start += DataOption.Length;
        var end = binaryPath.IndexOf('"', start);
        return end > start ? binaryPath[start..end] : null;
    }

    /// <summary>Priecinok nastaveni nainstalovanej sluzby; null, ak sluzba nie je nainstalovana.</summary>
    public static string? InstalledDataFolder()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        // ImagePath citaju aj bezni pouzivatelia (sc.exe qc by chcelo len viac prace)
        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{Name}");
        return key?.GetValue("ImagePath") is string binaryPath ? DataFolderOf(binaryPath) ?? DefaultDataFolder : null;
    }

    /// <summary>Sluzba bezi alebo sa prave spusta (pri starte Windows s oneskorenim).</summary>
    public static bool IsStartingOrRunning()
    {
        if (!OperatingSystem.IsWindows())
            return false;
        try
        {
            using var service = new ServiceController(Name);
            return service.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending or ServiceControllerStatus.ContinuePending;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Volania <c>sc.exe</c> na zalozenie (alebo zmenu existujucej) a nastavenie sluzby.</summary>
    public static IReadOnlyList<string[]> InstallCommands(string binaryPath, bool exists) =>
    [
        [exists ? "config" : "create", Name, "binPath=", binaryPath, "start=", "delayed-auto", "DisplayName=", DisplayName],
        ["description", Name, Description],
        // po zlyhani (aj pri restarte z webu - kod ukoncenia rozny od 0) ju Windows spusti znova
        ["failure", Name, "reset=", "86400", "actions=", "restart/5000/restart/10000/restart/60000"],
        ["failureflag", Name, "1"],
        ["sdset", Name, Security]
    ];

    /// <summary>
    /// Nainstaluje (alebo zmeni) a spusti sluzbu a pocka, kym server otvori web; vrati kod ukoncenia programu
    /// (<see cref="NotStartedExitCode" />, ak sa server nespustil).
    /// </summary>
    public static async Task<int> InstallAsync(CommandLine commandLine, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(output);
        if (!CheckAdministrator(output))
            return 2;

        var dataFolder = commandLine.Paths.DataFolder;
        Directory.CreateDirectory(dataFolder);
        // kluce sifrovane pre pouzivatela by ucet sluzby neprecital (prihlasenie, heslo certifikatu)
        var converted = KeyRingProtection.ToLocalMachine(commandLine.Paths.KeysFolder, out var failed);
        if (converted > 0)
            output.WriteLine($"Kluce ochrany dat prevedene pre pocitac: {converted}");
        foreach (var file in failed)
            output.WriteLine($"Kluc {file} patri inemu pouzivatelovi - sluzba ho nepouzije (prihlasenia a heslo certifikatu treba zadat znova).");

        using var existing = Find();
        if (existing is not null && existing.Status != ServiceControllerStatus.Stopped)
        {
            existing.Stop();
            existing.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
        }

        var binaryPath = BinaryPath(Environment.ProcessPath!, dataFolder, commandLine.Port);
        foreach (var command in InstallCommands(binaryPath, existing is not null))
            if (Sc(command, output) != 0)
                return 1;

        // stav Running hlasi sluzba skor, nez server otvori port - caka sa na adresu v zamku priecinka alebo na dovod zlyhania
        var since = DateTime.UtcNow;
        var started = TryStart() is { } error ? new ServerStart(null, error) : await WaitForServerAsync(dataFolder, since, StartTimeout).ConfigureAwait(false);
        if (started.Url is null)
        {
            output.WriteLine($"Sluzba {Name} je nainstalovana, ale server sa nespustil: " +
                             (started.Error ?? $"ani po {(int)StartTimeout.TotalSeconds} s neotvoril web - pozrite log v {commandLine.Paths.LogsFolder}"));
            output.WriteLine("Windows ju skusi spustit znova. Inak ju spusti odkaz TableSimulator v ponuke Start.");
            return NotStartedExitCode;
        }

        output.WriteLine($"Sluzba {Name} bezi: {started.Url} (nastavenia: {dataFolder}). Spusta sa so systemom; zastavi ju --uninstall-service.");
        return 0;
    }

    /// <summary>Spusti sluzbu (bez cakania); vrati chybu alebo null. Uz bieziaca sluzba nie je chyba.</summary>
    public static string? TryStart()
    {
        if (!OperatingSystem.IsWindows())
            return "Sluzba Windows bezi len vo Windows.";
        try
        {
            using var service = new ServiceController(Name);
            service.Start();
            return null;
        }
        catch (InvalidOperationException e)
        {
            return e.InnerException is Win32Exception { NativeErrorCode: AlreadyRunning } ? null : e.InnerException?.Message ?? e.Message;
        }
    }

    /// <summary>
    /// Pocka, kym server sluzby zapise adresu do zamku priecinka nastaveni. Skonci skor, ked server zapise dovod
    /// zlyhania (<see cref="StartupError" />) alebo sa sluzba zastavi.
    /// </summary>
    /// <param name="dataFolder">priecinok nastaveni sluzby</param>
    /// <param name="sinceUtc">kedy sa sluzba spustila - starsi dovod zlyhania neplati</param>
    /// <param name="timeout">najdlhsie cakanie</param>
    public static async Task<ServerStart> WaitForServerAsync(string dataFolder, DateTime sinceUtc, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        DateTime? stoppedAt = null;
        while (true)
        {
            if (await ServerLock.ReadUrlAsync(dataFolder, TimeSpan.Zero).ConfigureAwait(false) is { } url)
                return new ServerStart(url, null);
            if (StartupError.ReadSince(dataFolder, sinceUtc) is { } error)
                return new ServerStart(null, error);
            if (IsStartingOrRunning())
                stoppedAt = null;
            else if ((stoppedAt ??= DateTime.UtcNow) + ReasonWait < DateTime.UtcNow)
                return new ServerStart(null, "Sluzba sa zastavila.");
            if (DateTime.UtcNow >= until)
                return new ServerStart(null, null);
            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
        }
    }

    /// <summary>Zastavi a odstrani sluzbu; nastavenia a databaza ostanu.</summary>
    public static int Uninstall(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!CheckAdministrator(output))
            return 2;
        using var existing = Find();
        if (existing is null)
        {
            output.WriteLine($"Sluzba {Name} nie je nainstalovana.");
            return 0;
        }

        if (existing.Status != ServiceControllerStatus.Stopped)
        {
            existing.Stop();
            existing.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
        }

        var result = Sc(["delete", Name], output);
        if (result == 0)
            output.WriteLine($"Sluzba {Name} je odstranena; nastavenia a databaza ostali.");
        return result == 0 ? 0 : 1;
    }

    [SupportedOSPlatform("windows")]
    private static ServiceController? Find()
    {
        var services = ServiceController.GetServices();
        var found = services.FirstOrDefault(s => string.Equals(s.ServiceName, Name, StringComparison.OrdinalIgnoreCase));
        foreach (var other in services.Where(s => s != found))
            other.Dispose();
        return found;
    }

    [SupportedOSPlatformGuard("windows")]
    private static bool CheckAdministrator(TextWriter output)
    {
        if (!OperatingSystem.IsWindows())
        {
            output.WriteLine("Sluzba sa da nainstalovat len vo Windows.");
            return false;
        }

        using var identity = WindowsIdentity.GetCurrent();
        if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            return true;
        output.WriteLine("Instalacia sluzby potrebuje prava spravcu - spustite prikazovy riadok ako spravca.");
        return false;
    }

    private static int Sc(string[] arguments, TextWriter output)
    {
        var info = new ProcessStartInfo("sc.exe") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var text = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            output.WriteLine($"sc {arguments[0]}: {text.Trim()}");
        return process.ExitCode;
    }
}
