using TableSimulator.Core.Boards;
using TableSimulator.Core.Boards.AkSignal;
using TableSimulator.Core.Boards.Chars;
using TableSimulator.Core.Boards.Elen;
using TableSimulator.Core.Boards.Flaps;
using TableSimulator.Core.Boards.RemoteAudio;
using TableSimulator.Core.Protocols.Elektrocas;

namespace TableSimulator.Core.Recording;

/// <summary>
/// Co tabula ukazuje ako riadky textu zhora (na porovnanie zaznamov): kazdy riadok profilu, prazdny riadok = "",
/// striedanie ako <c>{a|b}</c>; pohyblivy text ELEN ako posledny riadok <c>» text</c>. Pismo, farby a jas sa
/// neporovnavaju. Zosilnovac ELSVO je jeden riadok <c>ON 1</c>, <c>ON 2</c> alebo <c>OFF</c>, rozhlas vzdialeneho INISSu
/// riadok kazdej fronty.
/// </summary>
public static class BoardText
{
    /// <summary>Predpona riadku s pohyblivym textom.</summary>
    public const string MovingPrefix = "» ";

    public static IReadOnlyList<string> Rows(SimBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Display switch
        {
            ElenDisplay elen => Elen(elen, board.Profile.Rows),
            CharDisplay chars => Chars(chars, board.Profile.Rows, board.ManufacturerKey),
            FlapDisplay flaps => Flaps(flaps, board.Profile, board.ManufacturerKey),
            // zosilnovac ELSVO: zopnuty v pasme alebo vypnuty
            AmplifierDisplay amplifier => [amplifier.Band > 0 ? "ON " + amplifier.Band.ToString(System.Globalization.CultureInfo.InvariantCulture) : "OFF"],
            AkSignalDisplay akSignal => AkSignal(akSignal),
            RemoteAudioDisplay remote => Remote(remote),
            _ => []
        };
    }

    /// <summary>Ustredna AK Signal: riadok svetla a riadok kazdeho hrajuceho kanala rozhlasu.</summary>
    private static List<string> AkSignal(AkSignalDisplay display)
    {
        var light = new List<string> { "LIGHT" };
        if (display.Manual)
            light.Add("MAN");
        if (display.AutoMode)
            light.Add("AUTO");
        if (display.Dark)
            light.Add("DARK");
        light.Add(display.IsLightOn ? "ON" : "OFF");

        var text = new List<string> { string.Join(" ", light) };
        for (var channel = 0; channel <= 2; channel++)
        {
            if (display.Playing(channel) is not { } playing)
                continue;
            var name = channel == 0 ? "AUDIO" : "CH" + channel.ToString(System.Globalization.CultureInfo.InvariantCulture);
            text.Add(playing.Sounds.Count > 0 ? name + " " + string.Join("*", playing.Sounds) : name + " ON");
        }

        return text;
    }

    /// <summary>Rozhlas vzdialeneho INISSu: riadok kazdej fronty - vypnuta, volna alebo nahravky hrajuceho hlasenia.</summary>
    private static List<string> Remote(RemoteAudioDisplay display) =>
        display.Queues.Select(q => (q.Name.Length > 0 ? q.Name + " " : "")
                                   + (q.Off ? "OFF" : q.Playing is { } playing ? "PLAY " + string.Join("*", playing.Sounds) : "IDLE"))
            .ToList();

    private static List<string> Elen(ElenDisplay display, int rows)
    {
        // riadky ELEN su cislovane odspodu - zhora je najvyssie cislo
        var top = Math.Max(rows, display.Rows.Count == 0 ? 0 : display.Rows.Keys.Max());
        var text = new List<string>();
        for (var row = top; row >= 1; row--)
            text.Add(display.Rows.TryGetValue(row, out var content) ? content.PlainText.TrimEnd() : "");
        if (display.MovingText is { Length: > 0 } moving)
            text.Add(MovingPrefix + moving.Trim());
        return text;
    }

    private static List<string> Chars(CharDisplay display, int rows, string manufacturer)
    {
        var count = Math.Max(rows, display.Rows.Count == 0 ? 0 : display.Rows.Keys.Max() + 1);
        var text = new List<string>();
        for (var row = 0; row < count; row++)
            text.Add(display.Rows.TryGetValue(row, out var content) ? Row(content, manufacturer) : "");
        return text;
    }

    /// <summary>Listova tabula: texty listov modulov riadku oddelene <c> | </c>.</summary>
    private static List<string> Flaps(FlapDisplay display, BoardProfile profile, string manufacturer)
    {
        var count = Math.Max(profile.Rows, display.Rows.Count == 0 ? 0 : display.Rows.Keys.Max() + 1);
        var blank = FlapLabels.BlankCode(manufacturer);
        var text = new List<string>();
        for (var row = 0; row < count; row++)
        {
            var codes = display.Rows.GetValueOrDefault(row, "");
            var labels = FlapLabels.Modules(profile, row, codes.Length).Select(m => FlapLabels.Label(profile, m, codes, blank)).ToList();
            while (labels.Count > 0 && labels[^1].Length == 0)
                labels.RemoveAt(labels.Count - 1);
            text.Add(string.Join(" | ", labels));
        }

        return text;
    }

    /// <summary>Riadok znakovej tabule: zakladna vrstva, dalsie vrstvy striedania prekryte cez nu ako <c>{a|b}</c>.</summary>
    private static string Row(CharRow row, string manufacturer)
    {
        if (row.IsEmpty)
            return "";
        var length = row.Layers.Values.Max(l => l.Length);
        var base0 = row.Layers.TryGetValue(0, out var first) ? first : [];
        var parts = row.Layers.Keys.Order().Select(layer =>
        {
            var codes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                var cell = i < row.Layers[layer].Length && row.Layers[layer][i].IsSet ? row.Layers[layer][i]
                    : i < base0.Length && base0[i].IsSet ? base0[i] : new CharCell(0x20, 0);
                codes[i] = (byte)cell.Code;
            }

            return ElektrocasCharset.Decode(codes, manufacturer).TrimEnd();
        }).Distinct(StringComparer.Ordinal).ToList();
        return parts.Count == 1 ? parts[0] : "{" + string.Join("|", parts) + "}";
    }
}
