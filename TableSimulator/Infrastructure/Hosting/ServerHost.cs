using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using TableSimulator.Application.Server;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Proces servera - restart ako sluzba Windows: ukoncenie s kodom, po ktorom ju Windows spusti znova.</summary>
public sealed class ServerHost(IHostApplicationLifetime lifetime) : IServerHost
{
    public bool RunningAsService => WindowsService.IsRunning;

    public string Version { get; } =
        typeof(ServerHost).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";

    public IReadOnlyList<string> Addresses
    {
        get
        {
            // IPv4 ma prednost - meno pocitaca tablet alebo televizor casto nepreklada; najprv miestne siete
            // (192.168, 10, 172.16-31), potom ostatne (VPN); 169.254 (bez DHCP) nie
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                .Select(a => a.GetAddressBytes())
                .Where(b => !(b[0] == 169 && b[1] == 254))
                .OrderBy(LocalNetworkRank)
                .Select(b => new IPAddress(b).ToString())
                .Distinct();
            return [.. addresses, Dns.GetHostName()];
        }
    }

    private static int LocalNetworkRank(byte[] b) => b switch
    {
        [192, 168, ..] => 0,
        [10, ..] => 1,
        [172, >= 16 and <= 31, ..] => 2,
        _ => 3
    };

    public void Restart()
    {
        // kod ukoncenia sluzby = Environment.ExitCode; odpoved este odide
        Environment.ExitCode = WindowsService.RestartExitCode;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
            lifetime.StopApplication();
        });
    }
}
