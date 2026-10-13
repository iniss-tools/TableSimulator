using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TableSimulator.Application.Server;
using TableSimulator.Infrastructure.Auth;
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

    public static IServiceCollection AddMcp(this IServiceCollection services)
    {
        services.AddMcpServer(options => options.ServerInstructions = McpInstructions.Text)
            .WithHttpTransport()
            .AddAuthorizationFilters()
            .WithToolsFromAssembly(typeof(McpSetup).Assembly, ToolJson);
        services.AddOptions<McpServerOptions>().Configure<IServerHost>((options, host) =>
            options.ServerInfo = new Implementation { Name = "TableSimulator", Version = host.Version });
        services.AddAuthorizationBuilder().AddPolicy(Policy, policy => policy
            .AddAuthenticationSchemes(AccessKeyAuthenticationHandler.SchemeName)
            .RequireAuthenticatedUser());
        return services;
    }

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
