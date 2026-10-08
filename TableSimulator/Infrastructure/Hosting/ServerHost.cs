using System.Reflection;
using TableSimulator.Application.Server;

namespace TableSimulator.Infrastructure.Hosting;

/// <summary>Proces servera - restart ako sluzba Windows: ukoncenie s kodom, po ktorom ju Windows spusti znova.</summary>
public sealed class ServerHost(IHostApplicationLifetime lifetime) : IServerHost
{
    public bool RunningAsService => WindowsService.IsRunning;

    public string Version { get; } =
        typeof(ServerHost).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";

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
