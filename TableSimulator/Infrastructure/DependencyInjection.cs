using TableSimulator.Application.Alerts;
using TableSimulator.Application.Backup;
using TableSimulator.Application.Database;
using TableSimulator.Infrastructure.Alerts;
using TableSimulator.Application.Https;
using TableSimulator.Application.Live;
using TableSimulator.Application.Server;
using TableSimulator.Application.Settings;
using TableSimulator.Core.Simulation;
using TableSimulator.Core.Transport;
using TableSimulator.Core.Virtual;
using TableSimulator.Infrastructure.Backup;
using TableSimulator.Infrastructure.Hosting;
using TableSimulator.Infrastructure.Https;
using TableSimulator.Infrastructure.Persistence;
using TableSimulator.Infrastructure.Realtime;

namespace TableSimulator.Infrastructure;

/// <summary>Registracia infrastruktury servera: simulator, spojenia liniek, ulozenie liniek a tabul, WebSocket, HTTPS.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppPaths paths, ISettingsStore settings, ServerEndpoints endpoints)
    {
        services.AddSingleton(paths);
        services.AddSingleton(settings);
        services.AddSingleton(endpoints);
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddSingleton<HttpsCertificates>();
        services.AddSingleton<IHttpsCertificates>(sp => sp.GetRequiredService<HttpsCertificates>());
        services.AddSingleton(_ => new Simulator());
        services.AddSingleton<IServerHost, ServerHost>();
        services.AddSingleton<TransportManager>();
        services.AddSingleton<SimulatorConfigStore>();
        services.AddSingleton<IServerBackup, ServerBackup>();
        services.AddSingleton<IDatabaseSettings, DatabaseSettings>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<XmlTableFolders>();
        services.AddSingleton<LiveHub>();
        services.AddSingleton<ILiveNotifier>(sp => sp.GetRequiredService<LiveHub>());
        services.AddHostedService<SimulatorHostedService>();
        // za SimulatorHostedService - pri ukonceni servera zastavi scenare skor, nez sa linky a tabule ulozia
        services.AddHostedService<ScenarioHostedService>();
        services.AddHostedService<DesignWatcherService>();
        services.AddHostedService<XmlTableWatcherService>();
        services.AddHostedService<ReportMonitorService>();
        services.AddHostedService<AuditRetentionService>();
        services.AddHostedService<StatisticsService>();
        services.AddSingleton<IAlertChannel, WebhookAlertChannel>();
        services.AddSingleton<IAlertChannel, EmailAlertChannel>();
        services.AddHostedService<LineAlertService>();
        return services;
    }
}
