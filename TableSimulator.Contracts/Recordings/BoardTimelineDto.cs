namespace TableSimulator.Contracts.Recordings;

/// <summary>Co ukazovala tabula v zazname - stavy v case (zmeny do 1 s su jeden stav).</summary>
/// <param name="Id">identifikator <c>linka-adresa</c></param>
/// <param name="Duration">dlzka zaznamu [s]</param>
public sealed record BoardTimelineDto(string Id, string Name, double Duration, IReadOnlyList<BoardStateDto> States);
