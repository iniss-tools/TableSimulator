namespace TableSimulator.Core.Protocols.Apel;

/// <summary>Zaznam spravy APEL: prikaz (pri zapise textu cislo zasobnika alebo riadku) a jeho data.</summary>
/// <param name="Command">prikaz</param>
/// <param name="Data">data</param>
public sealed record ApelRecord(int Command, byte[] Data);
