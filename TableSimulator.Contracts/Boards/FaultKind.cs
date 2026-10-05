using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Boards;

/// <summary>Porucha, ktoru tabula predstiera.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FaultKind>))]
public enum FaultKind
{
    /// <summary>Bez poruchy.</summary>
    [JsonStringEnumMemberName("none")] None,

    /// <summary>Tabula neodpoveda.</summary>
    [JsonStringEnumMemberName("noReply")] NoReply,

    /// <summary>Tabula spravy odmieta.</summary>
    [JsonStringEnumMemberName("nak")] Nak,

    /// <summary>Tabula odpoveda oneskorene.</summary>
    [JsonStringEnumMemberName("delay")] Delay,

    /// <summary>Tabula odpoveda nezmyslom.</summary>
    [JsonStringEnumMemberName("garbage")] Garbage,

    /// <summary>Tabula hlasi poruchu v stave (tabule so stavovou odpovedou).</summary>
    [JsonStringEnumMemberName("status")] Status
}
