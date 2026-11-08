using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Predloha cela v zozname.</summary>
public sealed record FaceSummary(
    string Id,
    string Name,
    [property: Description("Built-in face; it cannot be changed, only copied.")]
    bool BuiltIn,
    [property: Description("Number of elements (areas, texts, clocks, symbols, images).")]
    int Elements);
