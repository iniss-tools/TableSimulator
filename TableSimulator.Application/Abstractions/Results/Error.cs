namespace TableSimulator.Application.Abstractions.Results;

/// <summary>
/// Chyba vysledku. <see cref="Code" /> je stabilny kluc (webove rozhranie podla neho zobrazi text v jazyku obsluhy),
/// <see cref="Description" /> je anglicky popis pre zaznam a klientov bez prekladu.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>Bez chyby.</summary>
    public static readonly Error None = new("", "", ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Problem(string code, string description) => new(code, description, ErrorType.Problem);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
}
