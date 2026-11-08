using System.ComponentModel;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Mcp.Results;

/// <summary>Prehlad virtualnych tabul.</summary>
public sealed record VirtualOverviewResult(
    [property: Description("Virtual boards: which INISS XML board (source) they show, which records (rowStart, rowCount; null = all), separator, layout or XSL template.")]
    IReadOnlyList<VirtualBoardDto> Boards,
    [property: Description("XML boards found in the INISS log folders; stale = INISS no longer refreshes the file (probably not running).")]
    IReadOnlyList<VirtualSourceDto> Sources,
    [property: Description("Watched INISS log folders.")]
    IReadOnlyList<VirtualFolderDto> Folders,
    IReadOnlyList<VirtualLayoutSummary> Layouts,
    [property: Description("Layout of boards without their own layout.")]
    string DefaultLayout,
    [property: Description("XSL templates in the INISSView folder set in the simulator; empty without it.")]
    IReadOnlyList<string> Templates);
