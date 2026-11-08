using System.Text.Json.Serialization;

namespace TableSimulator.Core.Boards.Faces;

/// <summary>Zvisle zarovnanie prvku cela.</summary>
public enum FaceVAlign
{
    /// <summary>Horny okraj prvku od horneho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("top")] Top,

    /// <summary>Stred prvku od stredu ukotvenia.</summary>
    [JsonStringEnumMemberName("middle")] Middle,

    /// <summary>Spodny okraj prvku od spodneho okraja ukotvenia.</summary>
    [JsonStringEnumMemberName("bottom")] Bottom
}
