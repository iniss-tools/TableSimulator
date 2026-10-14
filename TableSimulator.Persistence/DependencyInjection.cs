using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Faces;
using TableSimulator.Application.Keys;
using TableSimulator.Application.OAuth;
using TableSimulator.Application.Scenarios;
using TableSimulator.Application.Statistics;
using TableSimulator.Application.Signage;
using TableSimulator.Application.Virtual;
using TableSimulator.Persistence.Audit;
using TableSimulator.Persistence.Faces;
using TableSimulator.Persistence.Keys;
using TableSimulator.Persistence.OAuth;
using TableSimulator.Persistence.Scenarios;
using TableSimulator.Persistence.Statistics;
using TableSimulator.Persistence.Signage;
using TableSimulator.Persistence.Simulation;
using TableSimulator.Persistence.Postgres;
using TableSimulator.Persistence.Sqlite;
using TableSimulator.Persistence.Virtual;

namespace TableSimulator.Persistence;

/// <summary>Registracia databazy simulatora.</summary>
public static class DependencyInjection
{
    /// <summary>Databaza simulatora - SQLite alebo PostgreSQL podla <paramref name="database" />.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, DatabaseConnection database)
    {
        ArgumentNullException.ThrowIfNull(database);
        if (database.Provider == DatabaseProvider.Postgres)
            services.AddDbContext<AppDbContext, PostgresAppDbContext>(options => options.UseNpgsql(database.ConnectionString));
        else
            services.AddDbContext<AppDbContext, SqliteAppDbContext>(options => options.UseSqlite(database.ConnectionString));
        services.AddSingleton(database);
        services.AddScoped<SimulatorConfigRepository>();
        services.AddScoped<IVirtualBoardRepository, VirtualBoardRepository>();
        services.AddScoped<IVirtualLayoutRepository, VirtualLayoutRepository>();
        services.AddScoped<IFaceRepository, FaceRepository>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IAccessKeyRepository, AccessKeyRepository>();
        services.AddScoped<IOAuthRepository, OAuthRepository>();
        services.AddScoped<ISignageRepository, SignageRepository>();
        services.AddScoped<IFaultScenarioRepository, FaultScenarioRepository>();
        services.AddScoped<ITrafficStatisticsRepository, TrafficStatisticsRepository>();
        return services;
    }
}
