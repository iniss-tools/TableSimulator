using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.OAuth;

/// <summary>Chyby a kody chyb povoleni OAuth pre MCP.</summary>
public static class OAuthErrors
{
    public static Error RequestNotFound() =>
        Error.NotFound("oauth.requestNotFound", "The authorization request does not exist or has expired; connect the application again.");

    /// <summary>Pristup povoluje pouzivatel, nie program s klucom.</summary>
    public static Error AccessKey() => Error.Forbidden("oauth.accessKey", "Access cannot be granted with an access key.");

    public static Error RoleTooHigh() => Error.Forbidden("oauth.roleTooHigh", "You cannot grant a role with permissions you do not have yourself.");

    public static Error GrantNotFound(Guid id) => Error.NotFound("oauth.grantNotFound", $"Connected application {id} does not exist.");

    /// <summary>Cudzie povolenie zrusi len spravca klucov s rolou, ktora pokryva rolu povolenia.</summary>
    public static Error GrantForbidden() => Error.Forbidden("oauth.grantForbidden", "You cannot revoke this connected application.");
}
