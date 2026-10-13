namespace TableSimulator.Infrastructure.Auth;

/// <summary>Poziadavka z cudzej stranky: prehliadac posiela hlavicku Origin s inym serverom (aj pri DNS rebinding).</summary>
public static class RequestOrigin
{
    /// <summary>Bez hlavicky Origin (nie prehliadac) alebo z rovnakeho servera.</summary>
    public static bool IsSame(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var origin = request.Headers.Origin.ToString();
        return origin.Length == 0
               || Uri.TryCreate(origin, UriKind.Absolute, out var uri) && string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}
