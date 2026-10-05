namespace TableSimulator.Contracts.Errors;

/// <summary>
/// Jedna chyba v odpovedi API (ProblemDetails, vlastnost <c>errors</c>). <see cref="Code" /> je stabilny kluc - webove
/// rozhranie podla neho zobrazi text v jazyku obsluhy, <see cref="Description" /> je anglicky popis.
/// </summary>
public sealed record ErrorDto(string Code, string Description);
