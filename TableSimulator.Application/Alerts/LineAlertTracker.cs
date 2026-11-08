namespace TableSimulator.Application.Alerts;

/// <summary>
/// Kedy oznamit vypadok a navrat linky: vypadok az po <see cref="AlertSettings.DelaySeconds" /> (kratky restart INISSu
/// sa neoznami), navrat len po oznamenom vypadku a len pri znova pripojenom INISSe (zastavenie alebo zmena linky nie je
/// navrat). Linky, ktore sa v jednej kontrole pokazili alebo vratili, idu v jednom oznameni. Zmena druhu vypadku
/// (napr. ticho servera a potom odpojenie) je ten isty vypadok.
/// </summary>
public sealed class LineAlertTracker
{
    private readonly Dictionary<int, (DateTimeOffset Since, bool Notified, AlertLineState State)> _outages = [];

    /// <summary>Spracuje stav liniek v case <paramref name="now" />; vrati linky na oznamenie vypadku a navratu.</summary>
    public (IReadOnlyList<AlertLine> Outages, IReadOnlyList<AlertLine> Recoveries) Update(IReadOnlyList<AlertLineState> lines, DateTimeOffset now,
        AlertSettings settings)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(settings);
        var outages = new List<AlertLine>();
        var recoveries = new List<AlertLine>();
        foreach (var line in lines)
        {
            _outages.TryGetValue(line.Number, out var tracked);
            if (line.IsOutage)
            {
                var since = tracked.State is null ? line.Since : tracked.Since;
                var notified = tracked.Notified;
                if (!notified && now - since >= TimeSpan.FromSeconds(settings.DelaySeconds))
                {
                    outages.Add(new AlertLine(line.Number, line.Name, line.Status, line.Detail, since));
                    notified = true;
                }

                _outages[line.Number] = (since, notified, line);
            }
            else if (tracked.State is not null)
            {
                if (tracked.Notified && line.Status == "connected" && settings.Recovery)
                    recoveries.Add(new AlertLine(line.Number, line.Name, line.Status, line.Detail, line.Since) { Previous = tracked.State.Status });
                _outages.Remove(line.Number);
            }
        }

        // odstranena linka
        foreach (var number in _outages.Keys.Where(n => lines.All(l => l.Number != n)).ToList())
            _outages.Remove(number);
        return (outages, recoveries);
    }

    /// <summary>Zabudne vypadky (oznamenia sa vypli).</summary>
    public void Reset() => _outages.Clear();
}
