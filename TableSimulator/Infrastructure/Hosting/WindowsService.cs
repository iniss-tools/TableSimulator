using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Extensions.Hosting.WindowsServices;
using TableSimulator.Infrastructure.Auth;

namespace TableSimulator.Infrastructure.Hosting;

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

    private const string Description = "Simulator tabul pre INISS - linky k tabuliam, zobrazenie tabul a virtualne tabule na webe.";

    /// <summary>Proces bezi ako sluzba Windows.</summary>
    public static bool IsRunning => OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();

    /// <summary>Predvoleny priecinok nastaveni sluzby (bez <c>--data</c>) - ucet sluzby nema priecinok pouzivatela.</summary>
    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TableSimulator");

    /// <summary>Prikazovy riadok sluzby (cesta k programu a parametre).</summary>
    public static string BinaryPath(string program, string dataFolder, int? port)
    {
        var path = $"\"{program}\" --data \"{Path.TrimEndingDirectorySeparator(dataFolder)}\"";
        return port is { } p ? path + string.Create(CultureInfo.InvariantCulture, $" --port {p}") : path;
    }

    /// <summary>Volania <c>sc.exe</c> na zalozenie (alebo zmenu existujucej) a nastavenie sluzby.</summary>
    public static IReadOnlyList<string[]> InstallCommands(string binaryPath, bool exists) =>
    [
        [exists ? "config" : "create", Name, "binPath=", binaryPath, "start=", "delayed-auto", "DisplayName=", DisplayName],
        ["description", Name, Description],
        // po zlyhani (aj pri restarte z webu - kod ukoncenia rozny od 0) ju Windows spusti znova
        ["failure", Name, "reset=", "86400", "actions=", "restart/5000/restart/10000/restart/60000"],
        ["failureflag", Name, "1"]
    ];

    /// <summary>Nainstaluje (alebo zmeni) a spusti sluzbu; vrati kod ukoncenia programu.</summary>
    public static int Install(CommandLine commandLine, TextWriter output)
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

        using var service = new ServiceController(Name);
        service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
        output.WriteLine($"Sluzba {Name} bezi (nastavenia: {dataFolder}). Spusta sa so systemom; zastavi ju --uninstall-service.");
        return 0;
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
