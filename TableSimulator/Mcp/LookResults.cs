using System.ComponentModel;
using TableSimulator.Contracts.Boards;
using TableSimulator.Contracts.Designs;

namespace TableSimulator.Mcp;

/// <summary>Vzhlady tabul.</summary>
public sealed record DesignsResult(
    [property: Description("Designs; renderer led-matrix = dots (LED and LCD), flaps = split-flap boards.")]
    IReadOnlyList<DesignInfoDto> Designs);

/// <summary>Predlohy ciel tabul.</summary>
public sealed record FacesResult(IReadOnlyList<FaceSummary> Faces);

/// <summary>Predloha cela v zozname.</summary>
public sealed record FaceSummary(
    string Id,
    string Name,
    [property: Description("Built-in face; it cannot be changed, only copied.")]
    bool BuiltIn,
    [property: Description("Number of elements (areas, texts, clocks, symbols, images).")]
    int Elements);

/// <summary>Subory pisiem ELEN.</summary>
public sealed record FontsResult(
    IReadOnlyList<FontFileSummary> Files,
    [property: Description("Dot fonts for chars boards (LCD1, ERP) usable in profile.charFonts.")]
    IReadOnlyList<string> CharFonts);

/// <summary>Subor pisiem ELEN.</summary>
public sealed record FontFileSummary(
    [property: Description("File name for profile.fontFile.")]
    string File,
    string Name,
    IReadOnlyList<FontSummary> Fonts);

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

/// <summary>Vzhlad tabule po zmene.</summary>
public sealed record BoardLookResult(
    string Id,
    [property: Description("Design set for the board; null = default for the manufacturer.")]
    string? Design,
    [property: Description("Design actually used: the board's design, or the default design of the manufacturer when none is set or it does not exist.")]
    string DesignInUse,
    BoardProfileDto Profile,
    [property: Description("Page showing only this board; open it in a browser to see the result (with login enabled the page needs signing in).")]
    string ViewUrl);
