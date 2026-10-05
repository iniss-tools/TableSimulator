using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Audit;
using TableSimulator.Application.Keys;
using TableSimulator.Application.Virtual;
using TableSimulator.Persistence.Audit;
using TableSimulator.Persistence.Keys;
using TableSimulator.Persistence.Sqlite;
using TableSimulator.Persistence.Virtual;

namespace TableSimulator.Persistence;

/// <summary>Registracia databazy simulatora.</summary>
public static class DependencyInjection
{
    /// <summary>Databaza SQLite v subore <paramref name="databaseFile" />.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, string databaseFile)
    {
        var connection = new SqliteConnectionStringBuilder { DataSource = databaseFile, Pooling = false }.ToString();
        services.AddDbContext<AppDbContext, SqliteAppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IVirtualBoardRepository, VirtualBoardRepository>();
        services.AddScoped<IVirtualLayoutRepository, VirtualLayoutRepository>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IAccessKeyRepository, AccessKeyRepository>();
        return services;
    }
}
