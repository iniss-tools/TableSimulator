using System.ComponentModel;
using TableSimulator.Contracts.Virtual;

namespace TableSimulator.Mcp.Results;

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
