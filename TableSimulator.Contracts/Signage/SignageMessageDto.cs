namespace TableSimulator.Contracts.Signage;

/// <summary>Sprava informacneho panelu.</summary>
/// <param name="Id">identifikator</param>
/// <param name="Definition">obsah spravy</param>
/// <param name="MediaName">povodne meno nahrateho suboru; null bez suboru</param>
public sealed record SignageMessageDto(int Id, SetSignageMessageRequest Definition, string? MediaName);
