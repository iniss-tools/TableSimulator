using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Roles;

namespace TableSimulator.Application.Users;

/// <summary>Roly v databaze (implementuje server cez ASP.NET Core Identity); po zmene obnovi <see cref="RoleCatalog" />.</summary>
public interface IRoleService
{
    /// <summary>Vsetky roly s poctom pouzivatelov a klucov.</summary>
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Zalozi vlastnu rolu; vrati jej kluc.</summary>
    Task<Result<string>> CreateAsync(string displayName, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    /// <summary>Zmeni nazov (len vlastnej roly) a opravnenia.</summary>
    Task<Result> UpdateAsync(string name, string displayName, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    /// <summary>Odstrani vlastnu rolu, ak ju nema ziaden pouzivatel ani kluc.</summary>
    Task<Result> DeleteAsync(string name, CancellationToken cancellationToken);
}
