using System.ComponentModel;

namespace TableSimulator.Mcp.Results;

/// <summary>Subory pisiem ELEN.</summary>
public sealed record FontsResult(
    IReadOnlyList<FontFileSummary> Files,
    [property: Description("Dot fonts for chars boards (LCD1, ERP) usable in profile.charFonts.")]
    IReadOnlyList<string> CharFonts);
