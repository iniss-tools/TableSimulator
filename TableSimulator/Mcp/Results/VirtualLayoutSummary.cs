using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Rozlozenie v zozname.</summary>
public sealed record VirtualLayoutSummary(
    string Id,
    string Name,
    [property: Description("Built-in layout; it cannot be changed, only copied.")]
    bool BuiltIn,
    [property: Description("Number of columns; 0 = columns as in the XML.")]
    int Columns);
