using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Faces;

/// <summary>Vodorovne zarovnanie prvku cela.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FaceAlign>))]
public enum FaceAlign
{
    /// <summary>Lavy okraj prvku od laveho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("left")] Left,

    /// <summary>Stred prvku od stredu ukotvenia.</summary>
    [JsonStringEnumMemberName("center")] Center,

    /// <summary>Pravy okraj prvku od praveho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("right")] Right
}
