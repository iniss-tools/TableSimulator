namespace TableSimulator.Application.Auth;

/// <summary>Vysledok prihlasenia.</summary>
public enum SignInOutcome
{
    /// <summary>Prihlaseny.</summary>
    Success,

    /// <summary>Zle meno alebo heslo.</summary>
    InvalidCredentials,

    /// <summary>Docasne zablokovany po opakovanych neuspesnych pokusoch.</summary>
    LockedOut,

    /// <summary>Ucet je vypnuty.</summary>
    Disabled
}
