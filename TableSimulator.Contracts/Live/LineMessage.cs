using TableSimulator.Contracts.Lines;

namespace TableSimulator.Contracts.Live;

/// <summary>Zmenila sa linka (stav, pocitadla).</summary>
public sealed record LineMessage(LineDto Line) : LiveMessage;
