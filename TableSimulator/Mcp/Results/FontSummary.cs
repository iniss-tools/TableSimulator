using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Pismo v subore ELEN.</summary>
public sealed record FontSummary(
    [property: Description("Position of the font in the file: the face (bits 4-5 of the ELEN font number) or the extended font n; profile.fontMap can redirect them.")]
    int Index,
    string Name,
    [property: Description("Height in dots.")]
    int Height,
    [property: Description("Width of the widest character in dots.")]
    int MaxWidth,
    bool Proportional);
