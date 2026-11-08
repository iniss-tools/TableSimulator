using TableSimulator.Api;
using TableSimulator.Contracts.Errors;

namespace TableSimulator.Infrastructure.OpenApi;

/// <summary>
/// Chyba API tak, ako ju sklada <see cref="CustomResults" /> (ProblemDetails s kodom chyby) - len pre dokument OpenAPI.
/// Odpovede prihlasovania (401, 403) a neocakavane chyby kod nemaju, preto su vsetky vlastnosti nepovinne.
/// </summary>
public sealed record ApiProblem
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }

    /// <summary>Stabilny kluc chyby - webove rozhranie podla neho zobrazi text.</summary>
    public string? Code { get; init; }

    /// <summary>Chyby vstupov pri overeni.</summary>
    public IReadOnlyList<ErrorDto>? Errors { get; init; }
}
