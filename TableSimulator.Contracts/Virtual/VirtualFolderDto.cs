namespace TableSimulator.Contracts.Virtual;

/// <summary>Sledovany priecinok s XML tabul (priecinok logov INISSu).</summary>
/// <param name="Exists">priecinok je dostupny</param>
public sealed record VirtualFolderDto(string Path, bool Exists);
