using System.Collections.Concurrent;
using TableSimulator.Contracts.Signage;

namespace TableSimulator.Application.Signage;

/// <summary>Kedy sa ktory panel naposledy pytal na program (len v pamati - od startu servera).</summary>
public sealed class SignagePanelTracker
{
    /// <summary>Panel si program vypyta aspon raz za minutu; dlhsie ticho = nepripojeny.</summary>
    public static readonly TimeSpan OnlineFor = TimeSpan.FromSeconds(150);

    private readonly ConcurrentDictionary<int, (DateTimeOffset At, string Address)> _seen = new();

    public void Touch(int panel, string address, DateTimeOffset now) => _seen[panel] = (now, address);

    public SignagePanelStatusDto? Status(int panel, DateTimeOffset now) =>
        _seen.TryGetValue(panel, out var seen) ? new SignagePanelStatusDto(seen.At, seen.Address, now - seen.At < OnlineFor) : null;
}
