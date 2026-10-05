namespace TableSimulator.Contracts.Live;

/// <summary>Zmenili sa XML tabul alebo definicie virtualnych tabul - zobrazenia si obsah nacitaju znova.</summary>
/// <param name="Sources">mena zmenenych suborov XML (<c>SAVE_XML</c>)</param>
/// <param name="Definitions">zmenili sa definicie virtualnych tabul</param>
public sealed record VirtualMessage(IReadOnlyList<string> Sources, bool Definitions) : LiveMessage;
