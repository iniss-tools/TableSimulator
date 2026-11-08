using System.Net;
using Microsoft.AspNetCore.Http.Extensions;

namespace TableSimulator.Infrastructure.Https;

/// <summary>Presmerovanie pristupu zo siete na HTTPS.</summary>
public static class HttpsRedirectExtensions
{
    /// <summary>
    /// Poziadavka zo siete cez HTTP dostane presmerovanie 308 na rovnaku adresu cez HTTPS (aj zmena - 308 zachova metodu
    /// a telo). Z tohto pocitaca HTTP ostava - GVDEditor a prehliadac otvoreny serverom certifikat nepotrebuju.
    /// </summary>
    public static IApplicationBuilder UseHttpsForNetwork(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var endpoints = app.ApplicationServices.GetRequiredService<ServerEndpoints>();
        if (!endpoints.HttpsEnabled || !endpoints.HttpsRedirect)
            return app;
        return app.Use((context, next) =>
        {
            if (context.Request.IsHttps || IsLocal(context.Connection.RemoteIpAddress))
                return next(context);
            var request = context.Request;
            var host = request.Host.HasValue ? request.Host.Host : context.Connection.LocalIpAddress?.ToString() ?? "localhost";
            context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
            context.Response.Headers.Location = UriHelper.BuildAbsolute(Uri.UriSchemeHttps, new HostString(host, endpoints.HttpsPort), request.PathBase,
                request.Path, request.QueryString);
            return Task.CompletedTask;
        });
    }

    /// <summary>Spojenie z tohto pocitaca (TestServer adresu nema - tiez lokalne).</summary>
    private static bool IsLocal(IPAddress? address) =>
        address is null || IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);
}
