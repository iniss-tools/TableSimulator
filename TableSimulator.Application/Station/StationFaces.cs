using TableSimulator.Contracts.Station;
using TableSimulator.Core.Boards;
using ContractAlign = TableSimulator.Contracts.Faces.FaceAlign;

namespace TableSimulator.Application.Station;

/// <summary>Celo tabule z dat stanice: predloha (bez kluca), napoveda kluca a hodnoty premennych pre tabulu.</summary>
/// <param name="Face">predloha s nazvom, bez kluca</param>
/// <param name="IdHint">napoveda kluca predlohy (druh tabule a stanica)</param>
/// <param name="Values">hodnoty premennych cela pre tabulu</param>
public sealed record StationFace(BoardFace Face, string IdHint, IReadOnlyDictionary<string, string> Values);

/// <summary>
/// Celo tabule z dat stanice podla druhu tabule: odchodova a prichodova maju pas s nadpisom a hodinami, nastupistna
/// panel s cislom kolaje a pas s nazvom stanice a hodinami, podchodova panel s cislom nastupista. Nad stlpcami
/// zobrazenymi pri vlaku su popisy podla obsahu stlpca (slovensky alebo cesky a pod nim anglicky). Polohy su v bodoch
/// matice od jej laveho okraja ako v katalogu - nezavisia od sirky tabule, ktoru samoucenie moze zmenit.
/// </summary>
public static class StationFaces
{
    /// <summary>Premenna cela s cislom kolaje.</summary>
    public const string TrackKey = "track";

    /// <summary>Premenna cela s cislom nastupista.</summary>
    public const string PlatformKey = "platform";

    /// <summary>Premenna cela s nazvom stanice.</summary>
    public const string StationKey = "station";

    /// <summary>Riadkovanie viacriadkoveho textu (ako kreslenie cela na webe).</summary>
    private const double LineSpacing = 1.15;

    /// <summary>Priemerna sirka velkeho pismena voci velkosti pisma - na odhad sirky popisu.</summary>
    private const double CharWidth = 0.68;

    /// <summary>Vyska pasu s nadpisom odchodovej a prichodovej tabule.</summary>
    private const int TitleBand = 16;

    /// <summary>Najmensia medzera medzi popismi stlpcov.</summary>
    private const double LabelGap = 1.5;

    /// <summary>Vyska pasu s nazvom stanice nad nastupistnou tabulou.</summary>
    private const int StationBand = 15;

    /// <summary>Popisy stlpcov podla obsahu (cislo FILL_SECTION): slovensky, cesky, anglicky.</summary>
    private static readonly Dictionary<int, (string Sk, string Cs, string En)> ColumnLabels = new()
    {
        [2] = ("ZO STANICE", "ZE STANICE", "FROM"),
        [3] = ("CEZ", "PŘES", "VIA"),
        [4] = ("CEZ", "PŘES", "VIA"),
        [5] = ("CEZ", "PŘES", "VIA"),
        [6] = ("SMER", "SMĚR", "DIRECTION"),
        [7] = ("SMER", "SMĚR", "DIRECTION"),
        [8] = ("SMER", "SMĚR", "DIRECTION"),
        [9] = ("ODCHOD", "ODJEZD", "DEPARTURE"),
        [10] = ("PRÍCHOD", "PŘÍJEZD", "ARRIVAL"),
        [11] = ("MEŠKANIE", "ZPOŽDĚNÍ", "DELAY"),
        [12] = ("MEŠKANIE", "ZPOŽDĚNÍ", "DELAY"),
        [13] = ("MEŠKANIE", "ZPOŽDĚNÍ", "DELAY"),
        [14] = ("MEŠKANIE", "ZPOŽDĚNÍ", "DELAY"),
        [15] = ("VLAK", "VLAK", "TRAIN"),
        [16] = ("VLAK", "VLAK", "TRAIN"),
        [17] = ("VLAK", "VLAK", "TRAIN"),
        [18] = ("NÁZOV", "NÁZEV", "NAME"),
        [19] = ("KOĽAJ", "KOLEJ", "TRACK"),
        [20] = ("KOĽAJ", "KOLEJ", "TRACK"),
        [21] = ("NÁSTUPIŠTE", "NÁSTUPIŠTĚ", "PLATFORM"),
        [22] = ("NÁSTUPIŠTE", "NÁSTUPIŠTĚ", "PLATFORM"),
        [26] = ("VLAK", "VLAK", "TRAIN"),
        [27] = ("VLAK", "VLAK", "TRAIN"),
        [29] = ("VLAK", "VLAK", "TRAIN"),
        [30] = ("NÁST./KOĽ.", "NÁST./KOL.", "PLATF./TRACK"),
        [31] = ("NÁST./KOĽ.", "NÁST./KOL.", "PLATF./TRACK"),
        [32] = ("KOĽAJ", "KOLEJ", "TRACK"),
        [33] = ("KOĽAJ", "KOLEJ", "TRACK"),
        [34] = ("DOPRAVCA", "DOPRAVCE", "OPERATOR"),
        [35] = ("VLAK", "VLAK", "TRAIN"),
        [36] = ("LINKA", "LINKA", "LINE"),
        [37] = ("LINKA", "LINKA", "LINE"),
        [38] = ("VLAK", "VLAK", "TRAIN")
    };

    /// <summary>Celo tabule; null = druh tabule celo nema alebo by bolo prazdne.</summary>
    /// <param name="info">co o cele vedia data stanice</param>
    /// <param name="flaps">listova tabula - stlpce su v poziciach listov, popisy nad nimi sa nezakladaju</param>
    /// <param name="language">jazyk popisov</param>
    public static StationFace? Build(StationFaceDto info, bool flaps, StationFaceLanguage language)
    {
        ArgumentNullException.ThrowIfNull(info);
        var cs = language == StationFaceLanguage.Cs;
        var rowHeight = info.RowHeight > 0 ? info.RowHeight : 10;
        // popisy rastu s vyskou riadku (ELEN16 ma vacsie nez ELEN10)
        var scale = Math.Clamp(rowHeight / 10.0, 1, 1.5);
        // nastupistna ma nad maticou pas so stanicou, listova stlpce v poziciach listov
        var labels = flaps || info.Kind == StationBoardKind.Platform ? [] : Labels(info, cs, scale);
        var labelRoom = labels.Count > 0 ? (int)Math.Ceiling(LabelHeight(scale) + 2) : 0;
        var rows = Math.Max(1, info.Records * Math.Max(1, info.LinesPerRecord));
        var matrixHeight = rows * rowHeight + rows - 1;
        var station = info.Station.Trim();
        var track = Pick(info.Tracks);
        var platform = Pick(info.Platforms);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var elements = new List<FaceElement>();
        FaceMargin margin;
        string kindName, kindKey;
        switch (info.Kind)
        {
            case StationBoardKind.Departures or StationBoardKind.Arrivals:
            {
                var departures = info.Kind == StationBoardKind.Departures;
                (kindName, kindKey) = departures ? cs ? ("Odjezdová", "odjezdova") : ("Odchodová", "odchodova")
                    : cs ? ("Příjezdová", "prijezdova") : ("Príchodová", "prichodova");
                var title = departures ? cs ? "ODJEZDY / DEPARTURES" : "ODCHODY / DEPARTURES"
                    : cs ? "PŘÍJEZDY / ARRIVALS" : "PRÍCHODY / ARRIVALS";
                margin = new FaceMargin(Top: TitleBand + labelRoom);
                elements.Add(new FaceElement { Kind = FaceElementKind.Rect, Anchor = FaceAnchor.Top, Height = TitleBand - 2, Fill = "band" });
                elements.Add(new FaceElement
                {
                    Kind = FaceElementKind.Text, Anchor = FaceAnchor.Top, Align = FaceAlign.Center, Y = 2, Size = 10, Style = "title", Text = title
                });
                elements.Add(new FaceElement
                {
                    Kind = FaceElementKind.Clock, Anchor = FaceAnchor.Top, Align = FaceAlign.Right, X = 4, Y = 2.5, Size = 9, Render = FaceRender.Display
                });
                break;
            }
            case StationBoardKind.Platform:
            {
                (kindName, kindKey) = cs ? ("Nástupištní", "nastupistni") : ("Nástupištná", "nastupistna");
                // nad kolajou: panel s cislom kolaje, nad maticou pas s nazvom stanice a hodiny
                var left = track.Length > 0 ? Panel(elements, matrixHeight + StationBand, cs ? "KOLEJ" : "KOĽAJ", "TRACK", TrackKey) : 0;
                if (track.Length > 0)
                    values[TrackKey] = track;
                margin = new FaceMargin(Left: left, Top: StationBand);
                elements.Add(new FaceElement { Kind = FaceElementKind.Rect, Anchor = FaceAnchor.Matrix, Y = -StationBand, Height = StationBand - 3, Fill = "#dde2ea" });
                if (station.Length > 0)
                {
                    values[StationKey] = station;
                    elements.Add(new FaceElement
                    {
                        Kind = FaceElementKind.Text, Anchor = FaceAnchor.Matrix, X = 3, Y = -13.5, Size = 9, Bold = true, Color = "#1d4f9c",
                        MaxWidth = info.Width > 80 ? info.Width - 40 : 200, Text = "{" + StationKey + "}"
                    });
                }

                elements.Add(new FaceElement
                {
                    Kind = FaceElementKind.Clock, Anchor = FaceAnchor.Matrix, Align = FaceAlign.Right, X = 1, Y = -14, Size = 10,
                    Format = FaceClockFormat.HourMinute, Render = FaceRender.Display
                });
                break;
            }
            case StationBoardKind.Underpass:
            {
                (kindName, kindKey) = ("Podchodová", "podchodova");
                // pri vychode na nastupiste: panel s cislom nastupista (bez nastupista s cislom kolaje)
                var height = matrixHeight + labelRoom;
                var left = 0;
                if (platform.Length > 0)
                {
                    left = Panel(elements, height, cs ? "NÁSTUPIŠTĚ" : "NÁSTUPIŠTE", "PLATFORM", PlatformKey);
                    values[PlatformKey] = platform;
                }
                else if (track.Length > 0)
                {
                    left = Panel(elements, height, cs ? "KOLEJ" : "KOĽAJ", "TRACK", TrackKey);
                    values[TrackKey] = track;
                }

                if (left == 0 && labels.Count == 0)
                    return null;
                margin = new FaceMargin(Left: left, Top: labelRoom);
                break;
            }
            default:
                return null;
        }

        elements.AddRange(labels);
        var name = station.Length > 0 ? $"{kindName} – {station}" : kindName;
        var face = new BoardFace { Name = name.Length > 100 ? name[..100] : name, Margin = margin, Elements = elements };
        return new StationFace(face, station.Length > 0 ? $"{kindKey}-{station}" : kindKey, values);
    }

    /// <summary>
    /// Hodnota premennej z viacerych kolaji (nastupist): ta, ktora je zaciatkom vsetkych ostatnych (2 pre 2 a 2V),
    /// inak prva v poradi stanice.
    /// </summary>
    public static string Pick(IReadOnlyList<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var items = values.Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
        return items.Where(c => items.All(v => v.StartsWith(c, StringComparison.OrdinalIgnoreCase))).MinBy(c => c.Length)
               ?? items.FirstOrDefault() ?? "";
    }

    /// <summary>Panel s popisom a cislom vlavo od matice; vrati sirku miesta vlavo.</summary>
    private static int Panel(List<FaceElement> elements, double height, string label, string english, string key)
    {
        var width = (int)Math.Round(Math.Clamp(height * 0.9, 24, 36));
        var size = Half(Math.Clamp(height * 0.1, 3, 4.5));
        var number = Half(Math.Clamp(height - 4 - size * (1 + LineSpacing) - 5, 6, 26));
        elements.Add(new FaceElement
        {
            Kind = FaceElementKind.Rect, Anchor = FaceAnchor.Left, X = 2, Y = 2, Width = -3, Height = -2, Fill = "panel", Border = 0.6, Color = "#ffffff", Radius = 1
        });
        elements.Add(new FaceElement
        {
            Kind = FaceElementKind.Text, Anchor = FaceAnchor.Left, Align = FaceAlign.Center, X = -0.5, Y = 4, Size = size, Style = "label", MaxWidth = width - 8,
            Text = label + "\n" + english
        });
        elements.Add(new FaceElement
        {
            Kind = FaceElementKind.Text, Anchor = FaceAnchor.Left, Align = FaceAlign.Center, VAlign = FaceVAlign.Bottom, X = -0.5, Y = 4, Size = number,
            Style = "number", MaxWidth = width - 8, Text = "{" + key + "}"
        });
        return width;
    }

    /// <summary>
    /// Popisy nad stlpcami prveho riadku zaznamu. Susedne stlpce s rovnakym popisom (druh a cislo vlaku) maju jeden
    /// popis. Popis je zarovnany ako stlpec (sirka odhadnuta z dlzky textu) a moze zasahovat nad volnu cast susedneho
    /// stlpca; susedny popis neprekryje - radsej sa zuzi, popis zarovnany vpravo aj na koniec stlpca.
    /// </summary>
    private static List<FaceElement> Labels(StationFaceDto info, bool cs, double scale)
    {
        var groups = new List<(int Start, int End, ContractAlign Align, string Native, string English)>();
        foreach (var column in info.Columns.Where(c => c.Line == 0 && c.End > c.Start).OrderBy(c => c.Start))
        {
            if (!ColumnLabels.TryGetValue(column.Content, out var label))
                continue;
            var native = cs ? label.Cs : label.Sk;
            if (groups.Count > 0 && groups[^1].Native == native && groups[^1].English == label.En)
                groups[^1] = groups[^1] with { End = Math.Max(groups[^1].End, column.End) };
            else
                groups.Add((column.Start, column.End, column.Align, native, label.En));
        }

        if (groups.Count == 0)
            return [];

        var size = Math.Round(3.4 * scale, 1);
        var size2 = Math.Round(2.8 * scale, 1);
        var y = -Math.Round(LabelHeight(scale) + 1, 1);
        var width = Math.Max(info.Width, groups.Count > 0 ? groups.Max(g => g.End) : 0);
        // idealna poloha a sirka kazdeho popisu
        var placed = groups.Select(g =>
        {
            var estimate = CharWidth * Math.Max(g.Native.Length * size, g.English.Length * size2);
            var x = g.Align switch
            {
                ContractAlign.Right => g.End - estimate,
                ContractAlign.Center => (g.Start + g.End) / 2.0 - estimate / 2,
                _ => g.Start
            };
            return (X: Math.Max(0, x), Estimate: estimate);
        }).ToArray();

        // hranice medzi susednymi popismi: ak by sa idealne prekryli, hranica je v medzere medzi ich stlpcami
        var left = new double[groups.Count];
        var right = new double[groups.Count];
        right[^1] = width;
        for (var i = 0; i < groups.Count - 1; i++)
        {
            var end = placed[i].X + placed[i].Estimate;
            if (end + LabelGap > placed[i + 1].X)
            {
                var boundary = (groups[i].End + groups[i + 1].Start) / 2.0;
                right[i] = boundary - LabelGap / 2;
                left[i + 1] = boundary + LabelGap / 2;
            }
            else
            {
                right[i] = placed[i + 1].X - LabelGap;
                left[i + 1] = end + LabelGap;
            }
        }

        var elements = new List<FaceElement>();
        for (var i = 0; i < groups.Count; i++)
        {
            var x = Math.Ceiling(Math.Max(placed[i].X, left[i]) * 2) / 2;
            var limit = groups[i].Align == ContractAlign.Right ? Math.Min(right[i], groups[i].End) : right[i];
            var room = Math.Floor((limit - x) * 2) / 2;
            if (room < 4)
                continue;
            elements.Add(new FaceElement
            {
                Kind = FaceElementKind.Text, Anchor = FaceAnchor.Matrix, X = x, Y = y, Size = size, Size2 = size2, Style = "label", MaxWidth = room,
                Text = groups[i].Native + "\n" + groups[i].English
            });
        }

        return elements;
    }

    /// <summary>Vyska dvojriadkoveho popisu stlpca.</summary>
    private static double LabelHeight(double scale) => 3.4 * scale * LineSpacing + 2.8 * scale;

    private static double Half(double value) => Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2;
}
