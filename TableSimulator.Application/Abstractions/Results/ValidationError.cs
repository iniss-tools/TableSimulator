namespace TableSimulator.Application.Abstractions.Results;

/// <summary>Neplatne vstupy - jednotlive chyby su v <see cref="Errors" />.</summary>
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("validation", "One or more validation errors occurred.", ErrorType.Validation);
