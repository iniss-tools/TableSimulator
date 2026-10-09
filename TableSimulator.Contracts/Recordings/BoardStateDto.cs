namespace TableSimulator.Contracts.Recordings;

/// <summary>Co tabula ukazovala od casu <paramref name="Time" /> - riadky textu zhora (pohyblivy text ako <c>» text</c>).</summary>
/// <param name="Time">cas od zaciatku zaznamu [s], kedy sa tabula zacala menit</param>
/// <param name="Settled">kedy sa ustalila - posledny riadok stavu prisiel [s] (zobrazenie tabule v tomto stave)</param>
/// <param name="Rows">riadky; striedanie ako <c>{a|b}</c>, prazdny riadok ""</param>
public sealed record BoardStateDto(double Time, double Settled, IReadOnlyList<string> Rows);
