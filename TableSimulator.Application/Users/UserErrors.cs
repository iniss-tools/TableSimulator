using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

/// <summary>Chyby a kody chyb spravy pouzivatelov.</summary>
public static class UserErrors
{
    public const string Role = "users.role";

    public static Error NotFound(string userName) => Error.NotFound("users.notFound", $"User '{userName}' does not exist.");

    public static Error Self() => Error.Conflict("users.self", "You cannot change your own role, disable or delete yourself.");

    public static Error LastAdmin() => Error.Conflict("users.lastAdmin", "At least one enabled administrator must remain.");

    /// <summary>Pouzivatel nesmie pridelit ani spravovat opravnenia, ktore sam nema.</summary>
    public static Error RoleTooHigh() =>
        Error.Forbidden("users.roleTooHigh", "You cannot assign or manage a role with permissions you do not have yourself.");

    public static Error CurrentPassword() => Error.Problem("users.currentPassword", "The current password is not correct.");
}
