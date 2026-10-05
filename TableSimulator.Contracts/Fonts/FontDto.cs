namespace TableSimulator.Contracts.Fonts;

/// <summary>Pismo ELEN pre prehliadac: sirky znakov a bitove mapy znakov v base64.</summary>
public sealed record FontDto(string Name, int MaxWidth, int Height, bool Proportional, int BytesPerRow, int[] Widths, string[] Glyphs);
