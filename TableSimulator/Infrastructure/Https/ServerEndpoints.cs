using System.Globalization;
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
