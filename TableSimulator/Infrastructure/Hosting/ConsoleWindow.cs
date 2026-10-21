using System.Runtime.InteropServices;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>
/// Okno konzoly, ktore patri len tomuto procesu - program spusteny z ponuky Start alebo dvojklikom. Po chybe by sa
/// zatvorilo skor, nez si ju pouzivatel precita; v prikazovom riadku ani v sluzbe sa necaka.
/// </summary>
internal static partial class ConsoleWindow
{
    /// <summary>Pocka na klaves, ak okno konzoly patri len tomuto procesu.</summary>
    public static void PauseIfOwned(TextWriter output)
    {
        if (!OwnedByThisProcess())
            return;
        output.WriteLine("Okno zatvori lubovolny klaves.");
        Console.ReadKey(true);
    }

    private static bool OwnedByThisProcess()
    {
        // presmerovany vystup (instalator ho cita) nikto nevidi - caka sa len pri okne pre cloveka
        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected)
            return false;
        // zoznam procesov pripojenych ku konzole: len tento proces = konzolu otvorili Windows pre neho
        var buffer = Marshal.AllocHGlobal(2 * sizeof(uint));
        try
        {
            return GetConsoleProcessList(buffer, 2) == 1;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [LibraryImport("kernel32.dll")]
    private static partial uint GetConsoleProcessList(nint processList, uint processCount);
}
