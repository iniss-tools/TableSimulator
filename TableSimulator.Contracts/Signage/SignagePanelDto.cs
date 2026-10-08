namespace TableSimulator.Contracts.Signage;

/// <summary>Panel s tym, co prave hra.</summary>
/// <param name="Id">cislo (<c>#/panel/{id}</c>)</param>
/// <param name="Definition">nastavenie</param>
/// <param name="Status">posledne spojenie; null = od startu servera sa nepripojil</param>
/// <param name="Playing">sprava, ktoru skupina prave hra; null = mimo prevadzky</param>
public sealed record SignagePanelDto(int Id, SetSignagePanelRequest Definition, SignagePanelStatusDto? Status, int? Playing);
