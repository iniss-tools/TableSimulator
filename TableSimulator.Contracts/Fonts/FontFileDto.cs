namespace TableSimulator.Contracts.Fonts;

/// <summary>Subor pisiem ELEN (.bin) pre prehliadac.</summary>
public sealed record FontFileDto(string File, string Name, FontDto[] Fonts);
