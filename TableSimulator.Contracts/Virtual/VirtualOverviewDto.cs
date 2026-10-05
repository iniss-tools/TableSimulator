namespace TableSimulator.Contracts.Virtual;

/// <summary>Virtualne tabule, najdene XML tabul a sledovane priecinky (GET /api/virtual).</summary>
public sealed record VirtualOverviewDto(IReadOnlyList<VirtualBoardDto> Boards, IReadOnlyList<VirtualSourceDto> Sources, IReadOnlyList<VirtualFolderDto> Folders);
