using TableSimulator.Application.Abstractions.Results;

namespace TableSimulator.Application.Keys;

/// <summary>Chyby a kody chyb pristupovych klucov.</summary>
public static class KeyErrors
{
    public const string Name = "keys.name";
    public const string Kind = "keys.kind";
    public const string Role = "keys.role";

    public static Error NotFound(Guid id) => Error.NotFound("keys.notFound", $"Access key {id} does not exist.");
}
