using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Faces;

/// <summary>Vodorovne zarovnanie prvku cela.</summary>
public enum FaceAlign
{
    /// <summary>Lavy okraj prvku od laveho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("left")] Left,

    /// <summary>Stred prvku od stredu ukotvenia.</summary>
    [JsonStringEnumMemberName("center")] Center,

    /// <summary>Pravy okraj prvku od praveho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("right")] Right
}
