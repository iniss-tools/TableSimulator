using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Faces;

/// <summary>Druh prvku cela.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FaceElementKind>))]
public enum FaceElementKind
{
    /// <summary>Plocha - pas, panel, ramik.</summary>
    [JsonStringEnumMemberName("rect")] Rect,

    /// <summary>Text.</summary>
    [JsonStringEnumMemberName("text")] Text,

    /// <summary>Hodiny tabule.</summary>
    [JsonStringEnumMemberName("clock")] Clock,

    /// <summary>Symbol zo vstavanej sady (sipky).</summary>
    [JsonStringEnumMemberName("symbol")] Symbol,

    /// <summary>Obrazok (logo).</summary>
    [JsonStringEnumMemberName("image")] Image
}
