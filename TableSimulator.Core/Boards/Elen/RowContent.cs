using System.Text;

namespace TableSimulator.Core.Boards.Elen;

/// <summary>
/// Obsah riadku tabule tak, ako ho tabula pochopila zo spravy.
/// </summary>
/// <param name="Elements">useky a striedania v poradi spravy</param>
/// <param name="Raw">obsah riadku zo spravy (bez cisla riadku)</param>
public sealed record RowContent(IReadOnlyList<RowElement> Elements, byte[] Raw)
{
    /// <summary>Prazdny riadok.</summary>
    public static RowContent Empty { get; } = new([], []);

    /// <summary>Riadok bez textu.</summary>
    public bool IsEmpty => Elements.Count == 0;

    /// <summary>
    /// Cely text riadku na zobrazenie v logu: useky s vlastnou polohou oddelene <c> · </c>, striedanie ako <c>{a|b}</c>.
    /// </summary>
    public string PlainText
    {
        get
        {
            var text = new StringBuilder();
            foreach (var element in Elements)
            {
                var positioned = element switch
                {
                    RunElement r => r.Run.X != null,
                    AlternationElement a => a.Parts.Any(p => p.Count > 0 && p[0].X != null),
                    _ => false
                };
                if (positioned && text.Length > 0)
                    text.Append(" · ");

                text.Append(element switch
                {
                    RunElement r => r.Run.Text,
                    AlternationElement a => "{" + string.Join("|", a.Parts.Select(p => string.Concat(p.Select(r => r.Text)))) + "}",
                    _ => ""
                });
            }

            return text.ToString();
        }
    }
}
