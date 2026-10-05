namespace TableSimulator.Contracts.Live;

/// <summary>Zmenil sa subor vzhladu v priecinku vzhladov.</summary>
/// <param name="Id">vzhlad</param>
public sealed record DesignMessage(string Id) : LiveMessage;
