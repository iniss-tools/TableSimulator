using TableSimulator.Contracts.Boards;

namespace TableSimulator.Contracts.Frames;

/// <summary>Vysledok vlozenia spravy na linku ako od INISSu.</summary>
/// <param name="Replies">odpovede tabul (hex) - INISS by ich dostal po linke</param>
/// <param name="Boards">tabule linky, ktore sa spravou zmenili</param>
public sealed record SentFrameDto(IReadOnlyList<FrameReplyDto> Replies, IReadOnlyList<BoardTextDto> Boards);
