namespace TableSimulator.Contracts.Signage;

/// <summary>Polozka programu panelu - sprava a ako dlho trva.</summary>
public sealed record SignageSlotDto(SignageMessageDto Message, int Duration);
