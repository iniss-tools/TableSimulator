using System.Globalization;

namespace TableSimulator.Application.Alerts;

/// <summary>Druh oznamenia.</summary>
public enum AlertKind
{
    Outage,
    Recovery,
    Test
}

/// <summary>Linka v oznameni.</summary>
/// <param name="Number">cislo linky</param>
/// <param name="Name">nazov linky</param>
/// <param name="Status">stav (kluc ako v API: <c>lost</c>, <c>error</c>, <c>connected</c>)</param>
/// <param name="Detail">podrobnost (chyba linky)</param>
/// <param name="Since">od kedy stav trva</param>
public sealed record AlertLine(int Number, string Name, string Status, string? Detail, DateTimeOffset Since);

/// <summary>Oznamenie pripravene na odoslanie - predmet a text v jazyku z nastavenia a udaje pre webhook.</summary>
public sealed record AlertMessage(AlertKind Kind, string Subject, string Text, IReadOnlyList<AlertLine> Lines, DateTimeOffset Time, string Server)
{
    /// <summary>Kluc druhu pre webhook.</summary>
    public string Event => Kind switch
    {
        AlertKind.Outage => "outage",
        AlertKind.Recovery => "recovery",
        _ => "test"
    };

    /// <summary>Zlozi oznamenie (texty slovensky alebo cesky).</summary>
    public static AlertMessage Create(AlertKind kind, IReadOnlyList<AlertLine> lines, DateTimeOffset time, string server, string language, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(zone);
        var cs = string.Equals(language, "cs", StringComparison.OrdinalIgnoreCase);
        string Clock(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, zone).ToString("d. M. yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        string Label(AlertLine l) => "Linka " + l.Number.ToString(CultureInfo.InvariantCulture) + (l.Name.Length > 0 ? " – " + l.Name : "");

        string subject;
        IEnumerable<string> body;
        switch (kind)
        {
            case AlertKind.Outage:
                subject = lines.Count == 1
                    ? (cs ? "Výpadek: " : "Výpadok: ") + Label(lines[0])
                    : (cs ? "Výpadek linek: " : "Výpadok liniek: ") + lines.Count.ToString(CultureInfo.InvariantCulture);
                body = lines.Select(l => Label(l) + ": " + (l.Status == "error"
                    ? (cs ? "linka neběží" : "linka nebeží") + (l.Detail is { Length: > 0 } d ? " – " + d : "")
                    : cs ? "INISS se odpojil" : "INISS sa odpojil") + (cs ? " (od " : " (od ") + Clock(l.Since) + ")");
                break;
            case AlertKind.Recovery:
                subject = lines.Count == 1
                    ? (cs ? "Znovu funguje: " : "Znova funguje: ") + Label(lines[0])
                    : (cs ? "Linky znovu fungují: " : "Linky znova fungujú: ") + lines.Count.ToString(CultureInfo.InvariantCulture);
                body = lines.Select(l => Label(l) + ": " + (cs ? "INISS je znovu připojen (" : "INISS je znova pripojený (") + Clock(l.Since) + ")");
                break;
            default:
                subject = cs ? "Zkušební oznámení" : "Skúšobné oznámenie";
                body = [cs ? "Oznámení o výpadcích linek simulátoru tabulí fungují." : "Oznámenia o výpadkoch liniek simulátora tabúľ fungujú."];
                break;
        }

        var footer = (cs ? "Simulátor tabulí na počítači " : "Simulátor tabúľ na počítači ") + server + ", " + Clock(time);
        return new AlertMessage(kind, "TableSimulator: " + subject, string.Join('\n', body) + "\n\n" + footer, lines, time, server);
    }
}
