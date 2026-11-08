using System.Globalization;

namespace TableSimulator.Application.Alerts;

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
                body = lines.Select(l => Label(l) + ": " + l.Status switch
                {
                    "error" => (cs ? "linka neběží" : "linka nebeží") + (l.Detail is { Length: > 0 } d ? " – " + d : ""),
                    AlertLineState.Silent => (cs ? "server INISS2 se neptá ústředny " : "server INISS2 sa nepýta ústredne ") + l.Detail
                                             + (cs ? " (poslední dotaz " : " (posledný dotaz ") + Clock(l.Since) + ")",
                    _ => cs ? "INISS se odpojil" : "INISS sa odpojil"
                } + (l.Status == AlertLineState.Silent ? "" : " (od " + Clock(l.Since) + ")"));
                break;
            case AlertKind.Recovery:
                subject = lines.Count == 1
                    ? (cs ? "Znovu funguje: " : "Znova funguje: ") + Label(lines[0])
                    : (cs ? "Linky znovu fungují: " : "Linky znova fungujú: ") + lines.Count.ToString(CultureInfo.InvariantCulture);
                body = lines.Select(l => Label(l) + ": " + (l.Previous == AlertLineState.Silent
                    ? cs ? "server INISS2 se ústředen znovu ptá" : "server INISS2 sa ústrední znova pýta"
                    : cs ? "INISS je znovu připojen (" + Clock(l.Since) + ")" : "INISS je znova pripojený (" + Clock(l.Since) + ")"));
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
