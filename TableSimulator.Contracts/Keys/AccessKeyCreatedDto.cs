namespace TableSimulator.Contracts.Keys;

/// <summary>Novy kluc a jeho tajna hodnota - server ju neuklada, ukaze sa len raz.</summary>
public sealed record AccessKeyCreatedDto(AccessKeyDto Key, string Secret);
