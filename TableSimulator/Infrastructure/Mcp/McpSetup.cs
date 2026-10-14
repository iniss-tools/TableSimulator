using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TableSimulator.Application.Server;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.OAuth;
using TableSimulator.Mcp;

namespace TableSimulator.Infrastructure.Mcp;

/// <summary>
/// Server MCP pre agentov (Streamable HTTP bez relacie): nastroje z <c>Mcp/</c> volaju handlery ako endpointy API a kazdy
/// ma opravnenie - klient vidi len nastroje svojej roly. Len s pristupovym klucom: rezim bez prihlasenia (spravca) tu neplati.
/// </summary>
public static class McpSetup
{
    /// <summary>Politika spojenia - overeny pristupovy kluc; co smie, urcuju opravnenia nastrojov.</summary>
    public const string Policy = "mcp";

    /// <summary>Adresa servera MCP.</summary>
    public const string Pattern = "/mcp";

    /// <summary>
    /// JSON vysledkov nastrojov: ako SDK, ale aj s hodnotami null - schema vystupu uvadza vsetky parametre konstruktora
    /// ako povinne, takze vynechana vlastnost by schemu porusila.
    /// </summary>
    private static readonly JsonSerializerOptions ToolJson = new(McpJsonUtilities.DefaultOptions) { DefaultIgnoreCondition = JsonIgnoreCondition.Never };

    /// <summary>
    /// Schemy nastrojov: <see cref="TimeOnly" /> a <see cref="DateTime" /> sa zapisu bez casoveho pasma, co formaty
    /// <c>time</c> a <c>date-time</c> (RFC 3339) nepripustaju - klient by vysledok odmietol, preto bez formatu.
    /// </summary>
    private static readonly AIJsonSchemaCreateOptions ToolSchemas = new()
    {
        TransformSchemaNode = (context, node) =>
        {
            var type = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;
            if ((type == typeof(TimeOnly) || type == typeof(DateTime)) && node is JsonObject schema)
                schema.Remove("format");
            return node;
        }
    };

    public static IServiceCollection AddMcp(this IServiceCollection services)
    {
        services.AddMcpServer(options => options.ServerInstructions = McpInstructions.Text)
            .WithHttpTransport()
            .AddAuthorizationFilters()
            .WithResources([typeof(AppPages)]);
        // ako WithToolsFromAssembly, ale s upravou schem
        foreach (var method in ToolMethods())
        {
            // _meta.ui nastroja so zobrazenim v klientovi (MCP Apps) podla [McpAppUi]
            services.AddSingleton(provider => McpApps.ApplyAppUiAttributes(McpServerTool.Create(method, (object?)null,
                new McpServerToolCreateOptions { Services = provider, SerializerOptions = ToolJson, SchemaCreateOptions = ToolSchemas })));
        }
        services.AddOptions<McpServerOptions>().Configure<IServerHost>((options, host) =>
            options.ServerInfo = new Implementation { Name = "TableSimulator", Version = host.Version });
        // kluc alebo token OAuth; bez nich vyzva 401 s odkazom na opis chraneneho zdroja (klient sa prihlasi cez OAuth)
        services.AddOAuth();
        services.AddAuthorizationBuilder().AddPolicy(Policy, policy => policy
            .AddAuthenticationSchemes(AccessKeyAuthenticationHandler.SchemeName, OAuthTokenAuthenticationHandler.SchemeName,
                OAuthEndpoints.ChallengeScheme)
            .RequireAuthenticatedUser());
        return services;
    }

    /// <summary>Staticke metody nastrojov z tried <see cref="McpServerToolTypeAttribute" /> v <c>Mcp/</c>.</summary>
    public static IReadOnlyList<MethodInfo> ToolMethods() =>
        typeof(McpSetup).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .Select(m => m.IsStatic ? m : throw new InvalidOperationException($"MCP tool {m.DeclaringType?.Name}.{m.Name} must be static."))
            .ToList();

    /// <summary>
    /// Spojenie <see cref="Pattern" />; poziadavka z cudzej stranky (hlavicka Origin s inym serverom) sa odmietne -
    /// ochrana pred DNS rebinding, ktoru specifikacia MCP vyzaduje.
    /// </summary>
    public static IEndpointConventionBuilder MapMcpServer(this IEndpointRouteBuilder app)
    {
        var builder = app.MapMcp(Pattern).RequireAuthorization(Policy).ExcludeFromDescription();
        builder.Add(endpoint =>
        {
            var inner = endpoint.RequestDelegate ?? throw new InvalidOperationException("MCP endpoint has no request delegate.");
            endpoint.RequestDelegate = context =>
            {
                if (RequestOrigin.IsSame(context.Request))
                    return inner(context);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        return builder;
    }
}
