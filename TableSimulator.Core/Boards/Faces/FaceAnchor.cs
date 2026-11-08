using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Faces;

/// <summary>Ukotvenie prvku cela.</summary>
public enum FaceAnchor
{
    /// <summary>Cela plocha vo vnutri ramu.</summary>
    [JsonStringEnumMemberName("face")] Face,

    /// <summary>Matica (vsetky riadky).</summary>
    [JsonStringEnumMemberName("matrix")] Matrix,

    /// <summary>Riadok tabule (<see cref="FaceElement.Row" />).</summary>
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
