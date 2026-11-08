using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Users;

/// <summary>Chyby a kody chyb roli.</summary>
public static class RoleErrors
{
    public const string Name = "roles.name";
    public const string Permissions = "roles.permissions";
    public const string View = "roles.view";

    public static Error NotFound(string name) => Error.NotFound("roles.notFound", $"Role '{name}' does not exist.");

    public static Error Admin() => Error.Conflict("roles.admin", "The administrator role always has all permissions and cannot be changed.");

    public static Error BuiltIn() => Error.Conflict("roles.builtIn", "A built-in role cannot be deleted.");

    public static Error Self() => Error.Conflict("roles.self", "You cannot change your own role.");

    public static Error TooHigh() => Error.Forbidden("roles.tooHigh", "You cannot grant or change permissions you do not have yourself.");

    public static Error Duplicate(string displayName) => Error.Conflict("roles.duplicate", $"A role named '{displayName}' already exists.");

    public static Error InUse(int users, int keys) =>
        Error.Conflict("roles.inUse", $"The role is assigned to {users} user(s) and {keys} access key(s) - assign them another role first.");
}
