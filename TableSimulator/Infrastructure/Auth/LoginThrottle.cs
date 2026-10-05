using System.Net;
using System.Net.Sockets;

namespace TableSimulator.Infrastructure.Auth;

/// <summary>
/// Limit neuspesnych pokusov o heslo z jednej adresy (prihlasenie aj zmena vlastneho hesla): po
/// <see cref="MaxFailures" /> neuspechoch za <see cref="Window" /> sa dalsie pokusy z adresy odmietnu bez overenia
/// hesla, kym najstarsi neuspech z okna nevypadne. Doplna zablokovanie uctu v Identity - to chrani jeden ucet, toto
/// skusanie mnohych uctov z jedneho miesta. Uspesne prihlasenie pocitadlo nenuluje (inak by ho utocnik nuloval
/// vlastnym uctom). Adresa IPv6 sa berie po sietach /64.
/// </summary>
public sealed class LoginThrottle(TimeProvider time, IHttpContextAccessor accessor)
{
    /// <summary>Pocet neuspechov v okne, po ktorom sa dalsie pokusy odmietaju.</summary>
    public const int MaxFailures = 10;

    /// <summary>Okno, v ktorom sa neuspechy zratavaju.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // ochrana pamate pri mnohych adresach - pri plnej tabulke sa vyhodia adresy bez neuspechu v okne
    private const int MaxAddresses = 10_000;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _failures = new(StringComparer.Ordinal);

    /// <summary>Kolko este z adresy aktualnej poziadavky treba cakat; null = moze skusat.</summary>
    public TimeSpan? Blocked()
    {
        var address = AddressOf(accessor.HttpContext?.Connection.RemoteIpAddress);
        var now = time.GetUtcNow();
        lock (_lock)
        {
            if (!_failures.TryGetValue(address, out var failures))
                return null;
            Prune(failures, now);
            if (failures.Count == 0)
            {
                _failures.Remove(address);
                return null;
            }

            return failures.Count >= MaxFailures ? failures.Peek() + Window - now : null;
        }
    }

    /// <summary>Zarata neuspesny pokus z adresy aktualnej poziadavky.</summary>
    public void Failed()
    {
        var address = AddressOf(accessor.HttpContext?.Connection.RemoteIpAddress);
        var now = time.GetUtcNow();
        lock (_lock)
        {
            if (!_failures.TryGetValue(address, out var failures))
            {
                if (_failures.Count >= MaxAddresses)
                    PruneAll(now);
                _failures[address] = failures = new Queue<DateTimeOffset>();
            }

            Prune(failures, now);
            failures.Enqueue(now);
            while (failures.Count > MaxFailures)
                failures.Dequeue();
        }
    }

    /// <summary>Kluc adresy: IPv4 (aj zabalena v IPv6) cela, IPv6 siet /64; bez adresy (TestServer) <c>local</c>.</summary>
    public static string AddressOf(IPAddress? address)
    {
        if (address is null)
            return "local";
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetworkV6 || IPAddress.IsLoopback(address))
            return address.ToString();
        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }

    private static void Prune(Queue<DateTimeOffset> failures, DateTimeOffset now)
    {
        while (failures.Count > 0 && failures.Peek() + Window <= now)
            failures.Dequeue();
    }

    private void PruneAll(DateTimeOffset now)
    {
        foreach (var (address, failures) in _failures.ToArray())
        {
            Prune(failures, now);
            if (failures.Count == 0)
                _failures.Remove(address);
        }
    }
}
