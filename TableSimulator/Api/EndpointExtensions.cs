using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace TableSimulator.Api;

/// <summary>Registracia a mapovanie endpointov API.</summary>
public static class EndpointExtensions
{
    /// <summary>Vsetky <see cref="IEndpoint" /> zostavenia.</summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly) =>
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo<IEndpoint>(), publicOnly: false)
            .As<IEndpoint>()
            .WithTransientLifetime());

    /// <summary>Namapuje endpointy do skupiny <paramref name="group" />.</summary>
    public static WebApplication MapEndpoints(this WebApplication app, RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(app);
        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
            endpoint.MapEndpoint(group);
        return app;
    }
}
