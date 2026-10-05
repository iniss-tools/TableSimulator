namespace TableSimulator.Contracts.Virtual;

/// <summary>Virtualne tabule, najdene XML tabul, sledovane priecinky a rozlozenia (GET /api/virtual).</summary>
/// <param name="DefaultLayout">rozlozenie tabul bez vlastneho rozlozenia</param>
public sealed record VirtualOverviewDto(
    IReadOnlyList<VirtualBoardDto> Boards,
    IReadOnlyList<VirtualSourceDto> Sources,
    IReadOnlyList<VirtualFolderDto> Folders,
    IReadOnlyList<VirtualLayoutDto> Layouts,
    string DefaultLayout);
