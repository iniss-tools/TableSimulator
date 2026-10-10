using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Auth;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Persistence;

/// <summary>Pri starte servera: migracie databazy, vstavane roly s predvolenymi opravneniami a katalog roli.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await ServerDatabase.MigrateAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), cancellationToken).ConfigureAwait(false);

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        foreach (var name in Roles.BuiltIn)
        {
            if (await roles.FindByNameAsync(name).ConfigureAwait(false) is not { } role)
                await roles.CreateAsync(new AppRole(name) { Permissions = Defaults(name) }).ConfigureAwait(false);
            // rola zo starsej verzie (pevne roly bez opravneni v databaze) dostane predvolene opravnenia
            else if (role.Permissions is null && Defaults(name) is { } defaults)
            {
                role.Permissions = defaults;
                await roles.UpdateAsync(role).ConfigureAwait(false);
            }
        }

        await LoadCatalogAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider.GetRequiredService<RoleCatalog>(),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Nacita roly z databazy do katalogu (pri starte a po zmene roly).</summary>
    public static async Task LoadCatalogAsync(AppDbContext db, RoleCatalog catalog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(catalog);
        var roles = await db.Roles.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        catalog.Load(roles.Where(r => r.Name is not null)
            .Select(r => new RoleInfo(r.Name!, r.DisplayName, r.Permissions ?? Roles.DefaultsOf(r.Name))));
    }

    // spravca ma opravnenia vzdy vsetky - neuklada ich, aby dostal aj opravnenia pridane neskor
    private static List<string>? Defaults(string role) => role == Roles.Admin ? null : [.. Roles.DefaultsOf(role)];
}
