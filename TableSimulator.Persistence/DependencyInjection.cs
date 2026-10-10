using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Keys;
using TableSimulator.Application.Scenarios;
using TableSimulator.Application.Statistics;
using TableSimulator.Application.Signage;
using TableSimulator.Application.Virtual;
using TableSimulator.Persistence.Audit;
using TableSimulator.Persistence.Keys;
using TableSimulator.Persistence.Scenarios;
using TableSimulator.Persistence.Statistics;
using TableSimulator.Persistence.Signage;
using TableSimulator.Persistence.Simulation;
using TableSimulator.Persistence.Sqlite;
using TableSimulator.Persistence.Virtual;

namespace TableSimulator.Persistence;

/// <summary>Registracia databazy simulatora.</summary>
public static class DependencyInjection
{
    /// <summary>Databaza SQLite v subore <paramref name="databaseFile" />.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string databaseFile)
    {
        services.AddDbContext<AppDbContext, SqliteAppDbContext>(options => options.UseSqlite(ServerDatabase.ConnectionString(databaseFile)));
        services.AddScoped<SimulatorConfigRepository>();
        services.AddScoped<IVirtualBoardRepository, VirtualBoardRepository>();
        services.AddScoped<IVirtualLayoutRepository, VirtualLayoutRepository>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IAccessKeyRepository, AccessKeyRepository>();
        services.AddScoped<ISignageRepository, SignageRepository>();
        services.AddScoped<IFaultScenarioRepository, FaultScenarioRepository>();
        services.AddScoped<ITrafficStatisticsRepository, TrafficStatisticsRepository>();
        return services;
    }
}
