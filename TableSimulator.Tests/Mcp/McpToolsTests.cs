using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using TableSimulator.Application.Abstractions.Messaging;
using TableSimulator.Application.Auth;
using TableSimulator.Contracts.Keys;
using TableSimulator.Infrastructure.Mcp;
using TableSimulator.Tests.Server;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace TableSimulator.Tests.Mcp;

/// <summary>
/// Pravidla nastrojov MCP (opravnenie, nazov, popisy, priznaky) a snimka <c>mcp-tools.json</c> - zmena toho, co model
/// vidi (popisy, schemy, pokyny servera), musi byt v diffe.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed partial class McpToolsTests
{
    /// <summary>Premenna prostredia: <c>1</c> = snimku zapisat namiesto porovnania.</summary>
    private const string UpdateVariable = "TABLESIMULATOR_UPDATE_MCP";

    // snimku cita clovek - apostrofy a diakritika bez \uXXXX
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public TestContext TestContext { get; set; } = null!;

    private static string SnapshotPath([CallerFilePath] string thisFile = "") => Path.Combine(Path.GetDirectoryName(thisFile)!, "mcp-tools.json");

    [GeneratedRegex("^[a-z]+(_[a-z]+)*$")]
    private static partial Regex SnakeCase();

    private static IReadOnlyList<MethodInfo> ToolMethods() => McpSetup.ToolMethods();

    /// <summary>Parameter, ktory dosadi server (handler, cas, zrusenie) - model ho nevidi.</summary>
    private static bool IsInjected(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(CancellationToken) || parameter.ParameterType == typeof(TimeProvider)
        || parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() is var definition
                                                  && (definition == typeof(IQueryHandler<,>) || definition == typeof(ICommandHandler<>) || definition == typeof(ICommandHandler<,>));

    [TestMethod]
    public void KazdyNastroj_MaOpravnenieNazovAPopisy()
    {
        var methods = ToolMethods();

        Assert.IsNotEmpty(methods);
        var policies = Permissions.All.Concat(Policies.AnyOf.Keys).ToHashSet();
        foreach (var method in methods)
        {
            var tool = method.GetCustomAttribute<McpServerToolAttribute>()!;
            var name = $"{method.DeclaringType!.Name}.{method.Name}";
            var authorize = method.GetCustomAttributes<AuthorizeAttribute>().ToList();
            Assert.HasCount(1, authorize, $"{name}: presne jedno [Authorize(Policy = Permissions.X)]");
            Assert.Contains(authorize[0].Policy ?? "", policies, $"{name}: neznama politika {authorize[0].Policy}");
            Assert.IsNull(method.GetCustomAttribute<AllowAnonymousAttribute>(), name);
            Assert.IsNotNull(tool.Name, $"{name}: nazov nastroja explicitne");
            Assert.IsTrue(SnakeCase().IsMatch(tool.Name), $"{name}: nazov {tool.Name} v snake_case");
            Assert.IsFalse(string.IsNullOrWhiteSpace(method.GetCustomAttribute<DescriptionAttribute>()?.Description), $"{name}: popis");
            Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Title), $"{name}: titulok");
            Assert.IsTrue(tool.UseStructuredContent, $"{name}: strukturovany vysledok");
            // priznaky pre klienta (automaticke povolenie len na citanie) - vzdy explicitne
            var named = method.GetCustomAttributesData().Single(a => a.AttributeType == typeof(McpServerToolAttribute)).NamedArguments
                .Select(a => a.MemberName).ToHashSet();
            foreach (var hint in new[] { nameof(McpServerToolAttribute.ReadOnly), nameof(McpServerToolAttribute.OpenWorld) })
                Assert.Contains(hint, named, $"{name}: {hint} explicitne");
            foreach (var parameter in method.GetParameters().Where(p => !IsInjected(p)))
                Assert.IsFalse(string.IsNullOrWhiteSpace(parameter.GetCustomAttribute<DescriptionAttribute>()?.Description), $"{name}: popis parametra {parameter.Name}");
        }
    }

    [TestMethod]
    public void NazvyNastrojov_SuJedinecne()
    {
        var names = ToolMethods().Select(m => m.GetCustomAttribute<McpServerToolAttribute>()!.Name).ToList();

        CollectionAssert.AllItemsAreUnique(names);
    }

    [TestMethod]
    public async Task Nastroje_ZodpovedajuSnimke()
    {
        await using var server = SimulatorServer.Start();
        var created = await server.Client().PostAsJsonAsync("/api/keys", new CreateAccessKeyRequest("Snimka", AccessKeyKind.Api, Roles.Admin),
            TestContext.CancellationToken);
        var secret = (await created.Content.ReadFromJsonAsync<AccessKeyCreatedDto>(TestContext.CancellationToken))!.Secret;
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.Server.BaseAddress, "mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + secret }
        }, server.CreateClient(), ownsHttpClient: true);
        await using var mcp = await McpClient.CreateAsync(transport, cancellationToken: TestContext.CancellationToken);

        var tools = await mcp.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
        var document = new JsonObject
        {
            ["instructions"] = mcp.ServerInstructions,
            ["tools"] = new JsonArray(tools.OrderBy(t => t.Name, StringComparer.Ordinal)
                .Select(t => JsonSerializer.SerializeToNode(t.ProtocolTool, McpJsonUtilities.DefaultOptions)).ToArray())
        };

        var path = SnapshotPath();
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await File.WriteAllTextAsync(path, document.ToJsonString(Indented).ReplaceLineEndings("\n") + "\n", TestContext.CancellationToken);
            return;
        }

        Assert.IsTrue(File.Exists(path), $"chyba {path}");
        var snapshot = JsonNode.Parse(await File.ReadAllTextAsync(path, TestContext.CancellationToken));
        Assert.IsTrue(JsonNode.DeepEquals(document, snapshot), $"Mcp/mcp-tools.json nezodpoveda serveru - spustite test s {UpdateVariable}=1");
    }
}
