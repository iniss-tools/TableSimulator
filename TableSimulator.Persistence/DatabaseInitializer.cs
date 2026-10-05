using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Auth;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Persistence;

/// <summary>Pri starte servera: migracie databazy a pevne roly.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var folder = Path.GetDirectoryName(db.Database.GetDbConnection().DataSource);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role).ConfigureAwait(false))
                await roles.CreateAsync(new AppRole(role)).ConfigureAwait(false);
        }
    }
}
