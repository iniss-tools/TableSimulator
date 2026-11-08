using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Faces;

/// <summary>Druh prvku cela.</summary>
public enum FaceElementKind
{
    /// <summary>Plocha - pas, panel, ramik.</summary>
    [JsonStringEnumMemberName("rect")] Rect,

    /// <summary>Text.</summary>
    [JsonStringEnumMemberName("text")] Text,

    /// <summary>Hodiny tabule.</summary>
    [JsonStringEnumMemberName("clock")] Clock,

    /// <summary>Symbol zo vstavanej sady (sipky smerovych tabuliek).</summary>
    [JsonStringEnumMemberName("symbol")] Symbol,

    /// <summary>Obrazok - logo dopravcu, znacka.</summary>
    [JsonStringEnumMemberName("image")] Image
}
