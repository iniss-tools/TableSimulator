using TableSimulator.Application.Abstractions.Results;
using TableSimulator.Contracts.Users;

namespace TableSimulator.Application.Users;

/// <summary>
/// Sprava pouzivatelov - implementuje server (ASP.NET Core Identity). Chrani prihlaseneho pred zmenou vlastnej roly,
/// vypnutim a odstranenim seba a simulator pred stratou posledneho spravcu, ktory sa moze prihlasit.
/// </summary>
public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);

    Task<Result> CreateAsync(string userName, string displayName, string role, string password, CancellationToken cancellationToken);

    /// <summary>Zmena mena, roly a vypnutia; zmena roly plati do minuty bez odhlasenia, vypnutie odhlasi.</summary>
    Task<Result> UpdateAsync(string userName, string displayName, string role, bool disabled, CancellationToken cancellationToken);

    /// <summary>Nove heslo od spravcu - zrusi zablokovanie aj prihlasenia pouzivatela.</summary>
    Task<Result> SetPasswordAsync(string userName, string password, CancellationToken cancellationToken);

    /// <summary>Zrusi zablokovanie po neuspesnych pokusoch.</summary>
    Task<Result> UnlockAsync(string userName, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(string userName, CancellationToken cancellationToken);

    /// <summary>Zmena vlastneho hesla prihlaseneho pouzivatela; prihlasenie ostane platne.</summary>
    Task<Result> ChangeOwnPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);
}
