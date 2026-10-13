using System.ComponentModel;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Mcp;

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

/// <summary>Rozlozenie v zozname.</summary>
public sealed record VirtualLayoutSummary(
    string Id,
    string Name,
    [property: Description("Built-in layout; it cannot be changed, only copied.")]
    bool BuiltIn,
    [property: Description("Number of columns; 0 = columns as in the XML.")]
    int Columns);

/// <summary>Co virtualna tabula prave ukazuje.</summary>
public sealed record VirtualBoardContent(
    VirtualBoardDto Board,
    [property: Description("The XML file the board shows; null = not found in the log folders.")]
    VirtualSourceDto? Source,
    [property: Description("Records shown, cells in the order of the INISS catalog table: name of the XML element (time, type, number, destination...) and text; "
                           + "parts separated by # alternate, a text ending with # blinks.")]
    IReadOnlyList<VirtualRowDto> Rows,
    [property: Description("Layout used to draw the board.")]
    string Layout,
    [property: Description("With an XSL template: why the page could not be built (noSource, notFound, template, failed); null = fine or no template.")]
    string? TemplateError,
    [property: Description("Page showing only this board; open it in a browser to see it (with login enabled the page needs signing in).")]
    string ViewUrl);
