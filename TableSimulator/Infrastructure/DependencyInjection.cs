using TableSimulator.Application.Live;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using TableSimulator.Infrastructure.Hosting;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Infrastructure.Realtime;

namespace TableSimulator.Infrastructure;

/// <summary>Registracia infrastruktury servera: simulator, spojenia liniek, ulozenie konfiguracie, WebSocket.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppPaths paths, ISettingsStore settings)
    {
        services.AddSingleton(paths);
        services.AddSingleton(settings);
        services.AddSingleton(_ => new Simulator());
        services.AddSingleton<TransportManager>();
        services.AddSingleton<ConfigStore>();
        services.AddSingleton<LiveHub>();
        services.AddSingleton<ILiveNotifier>(sp => sp.GetRequiredService<LiveHub>());
        services.AddHostedService<SimulatorHostedService>();
        services.AddHostedService<DesignWatcherService>();
        return services;
    }
}
