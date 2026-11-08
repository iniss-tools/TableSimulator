using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Subor pisiem ELEN.</summary>
public sealed record FontFileSummary(
    [property: Description("File name for profile.fontFile.")]
    string File,
    string Name,
    IReadOnlyList<FontSummary> Fonts);
