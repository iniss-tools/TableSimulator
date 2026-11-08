using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace TableSimulator.Infrastructure.Https;

/// <summary>Mena a adresy, pre ktore ma certifikat servera platit.</summary>
/// <param name="Dns">mena DNS</param>
/// <param name="Addresses">adresy IP</param>
public sealed record CertificateNames(IReadOnlyList<string> Dns, IReadOnlyList<IPAddress> Addresses)
{
    /// <summary>Mena a adresy ako text (najprv mena).</summary>
    public IReadOnlyList<string> All => [.. Dns, .. Addresses.Select(a => a.ToString())];

    /// <summary>
    /// Mena tohto pocitaca: <c>localhost</c>, meno pocitaca a plne meno v domene, adresa pre INISS
    /// (<paramref name="connectHost" />) a pri pristupe zo siete adresy IPv4 aktivnych sietovych rozhrani. IPv6 len
    /// <c>::1</c> - docasne adresy IPv6 sa menia a certifikat by sa vydaval stale znova.
    /// </summary>
    public static CertificateNames OfThisComputer(bool network, string connectHost)
    {
        var dns = new List<string> { "localhost" };
        var addresses = new List<IPAddress> { IPAddress.Loopback, IPAddress.IPv6Loopback };
        var machine = Environment.MachineName.ToLowerInvariant();
        AddName(dns, machine);
        try
        {
            if (IPGlobalProperties.GetIPGlobalProperties().DomainName is { Length: > 0 } domain)
                AddName(dns, $"{machine}.{domain.ToLowerInvariant()}");
            if (network)
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !addresses.Contains(unicast.Address))
                            addresses.Add(unicast.Address);
                }
            }
        }
        catch (NetworkInformationException)
        {
            // bez zoznamu rozhrani plati certifikat aspon pre meno pocitaca
        }

        var host = connectHost.Trim();
        if (IPAddress.TryParse(host, out var ip))
        {
            if (!addresses.Contains(ip))
                addresses.Add(ip);
        }
        else if (host.Length > 0 && Uri.CheckHostName(host) == UriHostNameType.Dns)
        {
            AddName(dns, host.ToLowerInvariant());
        }

        return new CertificateNames(dns, addresses);
    }

    /// <summary>Certifikat plati pre vsetky tieto mena a adresy.</summary>
    public bool CoveredBy(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        var extension = certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().FirstOrDefault();
        if (extension is null)
            return false;
        var dns = extension.EnumerateDnsNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addresses = extension.EnumerateIPAddresses().ToHashSet();
        return Dns.All(dns.Contains) && Addresses.All(addresses.Contains);
    }

    private static void AddName(List<string> names, string name)
    {
        if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
            names.Add(name);
    }
}
