using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;
using TableSimulator.Api;
using TableSimulator.Application.Auth;
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

/// <summary>
/// Dokument OpenAPI (<c>/api/openapi/v1.json</c>), z ktoreho sa generuje klient weboveho rozhrania (web/openapi.json):
/// vsetky vlastnosti su povinne (server zapisuje aj <c>null</c>, nullable urcuje C#), cisla su len cisla (citanie
/// z textu povolene vo <see cref="System.Text.Json.JsonSerializerDefaults.Web" /> klient nepouziva) a kazda operacia
/// ma chybovu odpoved <see cref="ApiProblem" />. Spravy WebSocketu <c>/ws</c> (<see cref="Contracts.Live.LiveMessage" />) dokument
/// nema - ich obalku ma webove rozhranie nad vygenerovanymi DTO.
/// </summary>
public static class ApiDocument
{
    /// <summary>Cesta dokumentu.</summary>
    public const string Route = "/api/openapi/{documentName}.json";

    private const string ProblemContentType = "application/problem+json";

    public static IServiceCollection AddApiDocument(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddSchemaTransformer(RequireProperties);
            options.AddDocumentTransformer(CompleteDocument);
        });

    /// <summary>Dokument pre kazdeho, kto vidi simulator (rovnake API ako webove rozhranie).</summary>
    public static IEndpointConventionBuilder MapApiDocument(this IEndpointRouteBuilder app) =>
        app.MapOpenApi(Route).RequireAuthorization(Permissions.SimulatorView);

    private static Task RequireProperties(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        // server zapisuje vsetky vlastnosti vratane null - v klientovi su povinne, null povoluje len typ
        if (schema.Properties is { Count: > 0 } properties && context.JsonTypeInfo.Type != typeof(ApiProblem))
            schema.Required = new HashSet<string>(properties.Keys, StringComparer.Ordinal);

        // cislo zapisane ako text server precita (JsonNumberHandling.AllowReadingFromString), ale posiela len cisla
        if (schema.Type is { } type && (type & (JsonSchemaType.Integer | JsonSchemaType.Number)) != 0 && (type & JsonSchemaType.String) != 0)
        {
            schema.Type = type & ~JsonSchemaType.String;
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }

    private static async Task CompleteDocument(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo { Title = "TableSimulator API", Version = "v1" };
        // adresa servera by zavisela od poziadavky (snimka dokumentu ma byt rovnaka)
        document.Servers = [];

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        document.Components.Schemas[nameof(ApiProblem)] = await context.GetOrCreateSchemaAsync(typeof(ApiProblem), null, cancellationToken);

        var problem = new OpenApiSchemaReference(nameof(ApiProblem), document);
        foreach (var operation in (document.Paths ?? []).Values.SelectMany(p => p.Operations?.Values.AsEnumerable() ?? []))
        {
            operation.Responses ??= [];
            operation.Responses["default"] = new OpenApiResponse
            {
                Description = "Error (ProblemDetails)",
                Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal) { [ProblemContentType] = new() { Schema = problem } }
            };
        }
    }
}
