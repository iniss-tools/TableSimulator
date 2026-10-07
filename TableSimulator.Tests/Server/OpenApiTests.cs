using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Application.Abstractions.Messaging;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Dokument OpenAPI, z ktoreho sa generuje klient weboveho rozhrania: snimka <c>web/openapi.json</c> musi zodpovedat
/// serveru a kazdy endpoint musi mat typ odpovede rovnaky ako jeho prikaz alebo dotaz.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class OpenApiTests
{
    /// <summary>Premenna prostredia: <c>1</c> = snimku zapisat namiesto porovnania.</summary>
    private const string UpdateVariable = "TABLESIMULATOR_UPDATE_OPENAPI";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private static string SnapshotPath([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "web", "openapi.json"));

    [TestMethod]
    public async Task Dokument_ZodpovedaSnimkeWebu()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();

        var document = JsonNode.Parse(await client.GetStringAsync("/api/openapi/v1.json"))!;

        var path = SnapshotPath();
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, document.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n");
            return;
        }

        Assert.IsTrue(File.Exists(path), $"chyba {path}");
        var snapshot = JsonNode.Parse(await File.ReadAllTextAsync(path));
        Assert.IsTrue(JsonNode.DeepEquals(document, snapshot),
            $"web/openapi.json nezodpoveda serveru - spustite test s {UpdateVariable}=1 a vo web/ bun run api");
    }

    [TestMethod]
    public async Task KazdyEndpoint_MaTypOdpovedePodlaHandlera()
    {
        await using var server = SimulatorServer.Start();

        var endpoints = server.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true && !e.RoutePattern.RawText.Contains("openapi", StringComparison.Ordinal))
            .ToList();

        Assert.IsGreaterThan(20, endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            var name = endpoint.RoutePattern.RawText;
            var produces = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
                .SingleOrDefault(p => p.StatusCode is >= 200 and < 300);
            Assert.IsNotNull(produces, $"{name} nema typ odpovede (.Produces)");

            var handler = endpoint.Metadata.GetMetadata<MethodInfo>()!.GetParameters().Select(p => p.ParameterType)
                .FirstOrDefault(t => t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                                                         t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                                                         t.GetGenericTypeDefinition() == typeof(ICommandHandler<>)));
            Assert.IsNotNull(handler, $"{name} nema handler");
            if (handler.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
                Assert.AreEqual(StatusCodes.Status204NoContent, produces.StatusCode, name);
            else if (produces.Type is not null && produces.Type != typeof(void))
                Assert.AreEqual(handler.GetGenericArguments()[1], produces.Type, name);
        }
    }
}
