using System.Text;

namespace TableSimulator.Application.OAuth;

/// <summary>Adresa navratu ku klientovi s parametrami odpovede povolenia.</summary>
public static class OAuthRedirect
{
    /// <summary><paramref name="uri" /> s pridanymi parametrami; null hodnoty vynecha.</summary>
    public static string With(string uri, params (string Name, string? Value)[] values)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(values);
        var builder = new StringBuilder(uri);
        var separator = uri.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        foreach (var (name, value) in values.Where(v => v.Value is not null))
        {
            builder.Append(separator).Append(name).Append('=').Append(Uri.EscapeDataString(value!));
            separator = '&';
        }
        return builder.ToString();
    }
}
