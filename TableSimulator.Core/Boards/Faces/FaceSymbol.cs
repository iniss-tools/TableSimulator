using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Faces;

/// <summary>Symbol cela - kresli sa vektorom, nie pismom, aby vyzeral rovnako v kazdom prehliadaci.</summary>
public enum FaceSymbol
{
    /// <summary>Sipka hore.</summary>
    [JsonStringEnumMemberName("arrowUp")] ArrowUp,

    /// <summary>Sipka dole.</summary>
    [JsonStringEnumMemberName("arrowDown")] ArrowDown,

    /// <summary>Sipka vlavo.</summary>
    [JsonStringEnumMemberName("arrowLeft")] ArrowLeft,

    /// <summary>Sipka vpravo.</summary>
    [JsonStringEnumMemberName("arrowRight")] ArrowRight,

    /// <summary>Sipka sikmo hore vlavo.</summary>
    [JsonStringEnumMemberName("arrowUpLeft")] ArrowUpLeft,

    /// <summary>Sipka sikmo hore vpravo.</summary>
    [JsonStringEnumMemberName("arrowUpRight")] ArrowUpRight,

    /// <summary>Sipka sikmo dole vlavo.</summary>
    [JsonStringEnumMemberName("arrowDownLeft")] ArrowDownLeft,

    /// <summary>Sipka sikmo dole vpravo.</summary>
    [JsonStringEnumMemberName("arrowDownRight")] ArrowDownRight,

    /// <summary>Dve sipky vlavo a vpravo (obojsmerne).</summary>
    [JsonStringEnumMemberName("arrowsLeftRight")] ArrowsLeftRight,

    /// <summary>Dve sipky hore a dole.</summary>
    [JsonStringEnumMemberName("arrowsUpDown")] ArrowsUpDown
}
