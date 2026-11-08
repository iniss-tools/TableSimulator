using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards;

/// <summary>
/// Porucha tabule, ktoru simulator predstiera - na skusku reakcie INISSu.
/// </summary>
public enum FaultKind
{
    /// <summary>Bez poruchy.</summary>
    [JsonStringEnumMemberName("none")] None,

    /// <summary>Tabula neodpoveda.</summary>
    [JsonStringEnumMemberName("noReply")] NoReply,

    /// <summary>Tabula kazdu spravu odmietne (NAK).</summary>
    [JsonStringEnumMemberName("nak")] Nak,

    /// <summary>Tabula odpovie oneskorene.</summary>
    [JsonStringEnumMemberName("delay")] Delay,

    /// <summary>Tabula odpovie nezmyslom.</summary>
    [JsonStringEnumMemberName("garbage")] Garbage,

    /// <summary>Tabula hlasi stav s poruchou (tabule so stavovou odpovedou, napr. LCD1).</summary>
    [JsonStringEnumMemberName("status")] Status
}
