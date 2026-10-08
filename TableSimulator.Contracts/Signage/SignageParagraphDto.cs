namespace TableSimulator.Contracts.Signage;

/// <summary>Odsek textovej spravy.</summary>
/// <param name="Text">text; novy riadok = zalomenie</param>
/// <param name="Color">farba <c>#rrggbb</c></param>
/// <param name="Size">velkost pisma</param>
/// <param name="Align">zarovnanie</param>
public sealed record SignageParagraphDto(string Text, string Color, SignageTextSize Size, SignageAlign Align);
