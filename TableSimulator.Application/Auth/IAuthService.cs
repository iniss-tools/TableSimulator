using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Auth;

/// <summary>Prihlasovanie pouzivatelov - implementuje server (ASP.NET Core Identity, cookie).</summary>
public interface IAuthService
{
    /// <summary>Server vyzaduje prihlasenie (nastavenie alebo pristup zo siete); inak je kazdy na tomto pocitaci spravca.</summary>
    bool LoginRequired { get; }

    /// <summary>Poziadavka prisla z tohto pocitaca.</summary>
    bool IsLocalRequest { get; }

    /// <summary>Existuje aspon jeden pouzivatel.</summary>
    Task<bool> HasUsersAsync(CancellationToken cancellationToken);

    /// <summary>Overi meno a heslo a prihlasi (cookie).</summary>
    Task<(SignInOutcome Outcome, SignedInUser? User)> SignInAsync(string userName, string password, bool remember, CancellationToken cancellationToken);

    /// <summary>Odhlasi.</summary>
    Task SignOutAsync();

    /// <summary>Zalozi prveho spravcu a prihlasi ho; chyby hesla alebo mena vrati ako chybu overenia.</summary>
    Task<Result<SignedInUser>> CreateFirstAdminAsync(string userName, string displayName, string password, CancellationToken cancellationToken);
}
