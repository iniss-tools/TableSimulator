using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;
using TableSimulator.Application.Keys;
using TableSimulator.Application.Users;
using TableSimulator.Contracts.Roles;
using TableSimulator.Persistence;
using TableSimulator.Persistence.Identity;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>Roly cez ASP.NET Core Identity; po kazdej zmene sa obnovi <see cref="RoleCatalog" />, takze opravnenia platia hned.</summary>
public sealed class RoleService(RoleManager<AppRole> roles, AppDbContext db, IAccessKeyRepository keys, RoleCatalog catalog) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await UserCountsAsync(cancellationToken).ConfigureAwait(false);
        var keyCounts = (await keys.ListAsync(cancellationToken).ConfigureAwait(false)).GroupBy(k => k.Role, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        return catalog.All.Select(r => new RoleDto(r.Name, r.DisplayName, Roles.IsBuiltIn(r.Name), r.Permissions, users.GetValueOrDefault(r.Name),
            keyCounts.GetValueOrDefault(r.Name))).ToArray();
    }

    public async Task<Result<string>> CreateAsync(string displayName, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        if (Duplicate(displayName, null))
            return Result.Failure<string>(RoleErrors.Duplicate(displayName));
        var existing = await roles.Roles.Select(r => r.Name!).ToListAsync(cancellationToken).ConfigureAwait(false);
        var role = new AppRole(RoleNames.Unique(displayName, existing)) { DisplayName = displayName, Permissions = [.. permissions] };
        var created = await roles.CreateAsync(role).ConfigureAwait(false);
        if (!created.Succeeded)
            return Result.Failure<string>(IdentityErrors.ToError(created.Errors));
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(role.Name!);
    }

    public async Task<Result> UpdateAsync(string name, string displayName, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        if (await roles.FindByNameAsync(name).ConfigureAwait(false) is not { } role)
            return Result.Failure(RoleErrors.NotFound(name));
        // vstavana rola ma nazov podla jazyka rozhrania
        if (!Roles.IsBuiltIn(name))
        {
            if (Duplicate(displayName, name))
                return Result.Failure(RoleErrors.Duplicate(displayName));
            role.DisplayName = displayName;
        }

        role.Permissions = [.. permissions];
        var updated = await roles.UpdateAsync(role).ConfigureAwait(false);
        if (!updated.Succeeded)
            return Result.Failure(IdentityErrors.ToError(updated.Errors));
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string name, CancellationToken cancellationToken)
    {
        if (await roles.FindByNameAsync(name).ConfigureAwait(false) is not { } role)
            return Result.Failure(RoleErrors.NotFound(name));
        var users = (await UserCountsAsync(cancellationToken).ConfigureAwait(false)).GetValueOrDefault(name);
        var keyCount = (await keys.ListAsync(cancellationToken).ConfigureAwait(false)).Count(k => k.Role == name);
        if (users > 0 || keyCount > 0)
            return Result.Failure(RoleErrors.InUse(users, keyCount));
        var deleted = await roles.DeleteAsync(role).ConfigureAwait(false);
        if (!deleted.Succeeded)
            return Result.Failure(IdentityErrors.ToError(deleted.Errors));
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>Nazov uz ma ina vlastna rola.</summary>
    private bool Duplicate(string displayName, string? except) =>
        catalog.All.Any(r => r.Name != except && !Roles.IsBuiltIn(r.Name) && string.Equals(r.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

    private async Task<Dictionary<string, int>> UserCountsAsync(CancellationToken cancellationToken)
    {
        var counts = await (from userRole in db.UserRoles
                group userRole by userRole.RoleId
                into g
                select new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var names = await db.Roles.Select(r => new { r.Id, r.Name }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return names.Where(n => n.Name is not null)
            .ToDictionary(n => n.Name!, n => counts.FirstOrDefault(c => c.RoleId == n.Id)?.Count ?? 0, StringComparer.Ordinal);
    }

    private Task ReloadAsync(CancellationToken cancellationToken) => DatabaseInitializer.LoadCatalogAsync(db, catalog, cancellationToken);
}
