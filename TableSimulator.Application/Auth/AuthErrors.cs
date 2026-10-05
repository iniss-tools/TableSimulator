using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Auth;

/// <summary>Chyby a kody chyb prihlasenia.</summary>
public static class AuthErrors
{
    public const string UserName = "auth.userName";
    public const string Password = "auth.password";
    public const string DisplayName = "auth.displayName";

    public static Error InvalidCredentials() => Error.Unauthorized("auth.invalidCredentials", "Invalid user name or password.");

    public static Error LockedOut() => Error.Unauthorized("auth.lockedOut", "Too many failed attempts - the account is temporarily locked.");

    public static Error Disabled() => Error.Unauthorized("auth.disabled", "The account is disabled.");

    public static Error NotRequired() => Error.Problem("auth.notRequired", "Login is not required on this server.");

    public static Error SetupDone() => Error.Conflict("auth.setupDone", "An administrator already exists.");

    public static Error SetupLocalOnly() => Error.Forbidden("auth.setupLocalOnly", "The first administrator can only be created on the simulator computer.");
}
