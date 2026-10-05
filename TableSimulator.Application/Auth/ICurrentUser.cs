namespace TableSimulator.Application.Auth;

/// <summary>Pouzivatel aktualnej poziadavky - implementuje server.</summary>
public interface ICurrentUser
{
    /// <summary>Poziadavka je prihlasena (pri vypnutom prihlasovani vzdy - ako spravca).</summary>
    bool IsAuthenticated { get; }

    /// <summary>Prihlasovacie meno; null = neprihlaseny.</summary>
    string? UserName { get; }

    /// <summary>Meno pre zobrazenie.</summary>
    string? DisplayName { get; }

    /// <summary>Rola; null = neprihlaseny.</summary>
    string? Role { get; }

    /// <summary>IP adresa, z ktorej poziadavka prisla; null = nezname.</summary>
    string? Address { get; }

    /// <summary>Poziadavka prisla s pristupovym klucom, nie s prihlasenim pouzivatela.</summary>
    bool IsAccessKey { get; }
}
