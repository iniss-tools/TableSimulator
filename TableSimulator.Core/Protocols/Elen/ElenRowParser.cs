using TableSimulator.Core.Boards;

namespace TableSimulator.Core.Protocols.Elen;

/// <summary>
/// Rozklad obsahu riadku ELEN na useky a striedania: kody polohy kurzora <c>10</c>–<c>13</c> (ELEKON aj <c>14</c>),
/// pismo <c>ESC n</c>, rozsirene pismo <c>19 h</c>, striedanie <c>ESC { *</c> … <c>|</c> … <c>}</c> a text.
/// Panel ELEN pouziva bajt za <c>{</c> ako pocitadlo striedania - moze byt lubovolny, INISS posiela <c>*</c>.
/// </summary>
public static class ElenRowParser
{
    private const byte Esc = 0x1B;
    private const byte ExtendedFont = 0x19;
    private const byte CursorFirst = 0x10;
    private const byte CursorLast = 0x13;
    private const byte WideCursorOrAttribute = 0x14;

    /// <summary>
    /// Rozlozi obsah riadku.
    /// </summary>
    /// <param name="content">obsah riadku zo spravy (za cislom riadku)</param>
    /// <param name="variant">variant protokolu (kodovanie, rozsirene pismo)</param>
    public static RowContent Parse(ReadOnlySpan<byte> content, ElenVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        var state = new State(variant);

        for (var i = 0; i < content.Length; i++)
        {
            var b = content[i];
            if (variant.CursorCodes && (b is >= CursorFirst and <= CursorLast || (b == WideCursorOrAttribute && variant.WideCursor))
                                     && i + 1 < content.Length)
            {
                state.Flush();
                state.X = (b - CursorFirst) * 128 + (content[++i] & 0x7F);
            }
            else if (b == WideCursorOrAttribute && variant.CursorCodes && i + 1 < content.Length)
            {
                // panel ELEN: 14 n meni farbu (bity 0-1) a pri bite 3 aj rez (bity 4-5)
                state.Flush();
                var n = content[++i];
                state.Font = (state.Font & ~0x03) | (n & 0x03);
                if ((n & 0x08) != 0)
                    state.Font = (state.Font & ~0x30) | (n & 0x30);
            }
            else if (b == Esc && i + 1 < content.Length)
            {
                var n = content[++i];
                if (n == (byte)'{' && i + 1 < content.Length)
                {
                    i++;
                    state.BeginAlternation();
                }
                else
                {
                    state.Flush();
                    state.Font = n;
                }
            }
            else if (b == ExtendedFont && variant.ExtendedFont && i + 1 < content.Length)
            {
                state.Flush();
                state.Font = (content[++i] << 8) | (state.Font & 0xFF);
            }
            else if (state.InAlternation && b == (byte)'|')
                state.NextPart();
            else if (state.InAlternation && b == (byte)'}')
                state.EndAlternation();
            else
                state.Codes.Add(b);
        }

        if (state.InAlternation)
            state.EndAlternation();
        state.Flush();
        return new RowContent(state.Elements, content.ToArray());
    }

    private sealed class State(ElenVariant variant)
    {
        public readonly List<RowElement> Elements = [];
        public readonly List<byte> Codes = [];
        public int? X;
        public int Font = variant.DefaultFont;

        private List<IReadOnlyList<TextRun>>? _parts;
        private List<TextRun>? _part;
        private int? _savedX;
        private int _savedFont;

        public bool InAlternation => _parts != null;

        public void Flush()
        {
            if (Codes.Count == 0)
                return;

            var codes = Codes.ToArray();
            var run = new TextRun(X, Font, codes, variant.Encoding.GetString(codes));
            Codes.Clear();
            X = null;

            if (_part != null)
                _part.Add(run);
            else
                Elements.Add(new RunElement(run));
        }

        public void BeginAlternation()
        {
            Flush();
            _parts = [];
            _part = [];
            _savedX = X;
            _savedFont = Font;
        }

        public void NextPart()
        {
            Flush();
            _parts!.Add(_part!);
            _part = [];
            X = _savedX;
            Font = _savedFont;
        }

        public void EndAlternation()
        {
            Flush();
            _parts!.Add(_part!);
            Elements.Add(new AlternationElement(_parts));
            _parts = null;
            _part = null;
            X = null;
            Font = _savedFont;
        }
    }
}
