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

    /// <summary>
    /// Namapuje endpointy do skupiny <paramref name="group" />. Nazov operacie v dokumente OpenAPI (a funkcie
    /// generovaneho klienta) je nazov triedy endpointu, oblast je jej priecinok v <c>Api/</c>.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app, RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(group);
        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
        {
            var type = endpoint.GetType();
            endpoint.MapEndpoint(group.MapGroup("").WithName(type.Name).WithTags(Area(type)));
        }

        return app;
    }

    /// <summary>Oblast endpointu - priecinok v <c>Api/</c> (<c>Lines</c>, <c>Boards</c>…).</summary>
    private static string Area(Type type)
    {
        var root = typeof(IEndpoint).Namespace!;
        var ns = type.Namespace ?? root;
        return ns.Length > root.Length ? ns[(root.Length + 1)..] : "Api";
    }
}
