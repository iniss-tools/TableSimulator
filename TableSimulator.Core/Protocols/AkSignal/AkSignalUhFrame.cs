using System.Text;

namespace TableSimulator.Core.Protocols.AkSignal;

/// <summary>
/// Prikaz servera ustredni AK Signal UH: text UTF-8 <c>&lt;prikaz|parameter|parameter&gt;</c>.
/// </summary>
/// <param name="Command">prikaz (<c>aud</c>, <c>sync</c>, <c>light</c>)</param>
/// <param name="Parameters">parametre za prikazom</param>
/// <param name="Raw">cela sprava</param>
public sealed record AkSignalUhFrame(string Command, IReadOnlyList<string> Parameters, byte[] Raw)
{
    /// <summary>Prikaz zvuku.</summary>
    public const string Audio = "aud";

    /// <summary>Prikaz zosuladenia bank zvukov.</summary>
    public const string Sync = "sync";

    /// <summary>Prikaz svetla.</summary>
    public const string Light = "light";

    /// <summary>Parameter zastavenia kanala.</summary>
    public const string Stop = "stop";

    /// <summary>Prikaz servera s parametrami.</summary>
    public static byte[] Build(string command, params string[] parameters) =>
        Encoding.UTF8.GetBytes("<" + string.Join("|", [command, .. parameters]) + ">");

    /// <summary>Odpoved ustredne <c>&lt;#prikaz|st=kluce&gt;</c>.</summary>
    public static byte[] Answer(string command, string state) => Encoding.UTF8.GetBytes("<#" + command + "|st=" + state + ">");

    /// <summary>Hodnota parametra <c>kluc=hodnota</c>; null = parameter nie je.</summary>
    public string? Value(string key) =>
        Parameters.FirstOrDefault(p => p.StartsWith(key + "=", StringComparison.Ordinal)) is { } found ? found[(key.Length + 1)..] : null;

    /// <summary>Text spravy z bajtov medzi <c>&lt;</c> a <c>&gt;</c>.</summary>
    internal static AkSignalUhFrame Parse(byte[] raw)
    {
        var text = Encoding.UTF8.GetString(raw, 1, raw.Length - 2);
        var parts = text.Split('|');
        return new AkSignalUhFrame(parts[0], parts[1..], raw);
    }
}
