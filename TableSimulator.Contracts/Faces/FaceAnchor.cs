using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Faces;

/// <summary>Ukotvenie prvku cela.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FaceAnchor>))]
public enum FaceAnchor
{
    /// <summary>Cela plocha vo vnutri ramu.</summary>
    [JsonStringEnumMemberName("face")] Face,

    /// <summary>Matica (vsetky riadky).</summary>
    [JsonStringEnumMemberName("matrix")] Matrix,

    /// <summary>Riadok tabule.</summary>
    [JsonStringEnumMemberName("row")] Row,

    /// <summary>Miesto nad maticou (sirka matice).</summary>
    [JsonStringEnumMemberName("top")] Top,

    /// <summary>Miesto pod maticou (sirka matice).</summary>
    [JsonStringEnumMemberName("bottom")] Bottom,

    /// <summary>Miesto vlavo od matice (cela vyska).</summary>
    [JsonStringEnumMemberName("left")] Left,

    /// <summary>Miesto vpravo od matice (cela vyska).</summary>
    [JsonStringEnumMemberName("right")] Right
}
