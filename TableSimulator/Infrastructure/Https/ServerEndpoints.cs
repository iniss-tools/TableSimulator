using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using TableSimulator.Application.Settings;

namespace TableSimulator.Infrastructure.Https;

/// <summary>
/// Porty a pristup, s ktorymi server bezi - z nastavenia pri starte (zmena sa prejavi po restarte). HTTP pocuva vzdy
/// (z tohto pocitaca ho pouziva GVDEditor aj otvoreny prehliadac), HTTPS na vlastnom porte, ak je nastaveny.
/// </summary>
/// <param name="HttpPort">port HTTP</param>
/// <param name="HttpsPort">port HTTPS; 0 = bez HTTPS</param>
/// <param name="HttpsRedirect">pristup zo siete cez HTTP presmerovat na HTTPS</param>
/// <param name="AllowNetwork">pocuvat na vsetkych adresach, nie len na tomto pocitaci</param>
public sealed record ServerEndpoints(int HttpPort, int HttpsPort, bool HttpsRedirect, bool AllowNetwork)
{
    /// <summary>Server pocuva aj na HTTPS.</summary>
    public bool HttpsEnabled => HttpsPort > 0;

    public static ServerEndpoints From(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new ServerEndpoints(settings.HttpPort, settings.HttpsPort, settings.HttpsRedirect, settings.AllowNetwork);
    }

    /// <summary>Adresa weboveho rozhrania na tomto pocitaci.</summary>
    public string LocalUrl => string.Create(CultureInfo.InvariantCulture, $"http://localhost:{HttpPort}/");

    /// <summary>Adresa HTTPS na tomto pocitaci; null bez HTTPS.</summary>
    public string? LocalHttpsUrl => HttpsEnabled ? string.Create(CultureInfo.InvariantCulture, $"https://localhost:{HttpsPort}/") : null;

    /// <summary>Kestrel: HTTP a HTTPS s certifikatom vybranym pri kazdom spojeni.</summary>
    public void Configure(KestrelServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Listen(options, HttpPort, null);
        if (HttpsEnabled)
        {
            Listen(options, HttpsPort, listen => listen.UseHttps(https =>
                https.ServerCertificateSelector = (_, _) => options.ApplicationServices.GetRequiredService<HttpsCertificates>().Current));
        }
    }

    private void Listen(KestrelServerOptions options, int port, Action<ListenOptions>? configure)
    {
        if (AllowNetwork)
            options.ListenAnyIP(port, listen => configure?.Invoke(listen));
        else
            options.ListenLocalhost(port, listen => configure?.Invoke(listen));
    }
}

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
