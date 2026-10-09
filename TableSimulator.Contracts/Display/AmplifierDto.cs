namespace TableSimulator.Contracts.Display;

/// <summary>Spinacia jednotka zosilnovaca ELSVO (rodina <c>amplifier</c>).</summary>
/// <param name="Band">pasmo zopnutia 1 alebo 2; 0 = vypnuty</param>
/// <param name="State">stav, ktorym jednotka odpoveda (bity ako v logu INISSu: 80 pripravena, 10 hlasenie…)</param>
/// <param name="OnSince">od kedy je zopnuty; null = vypnuty</param>
/// <param name="Switches">kolkokrat ho INISS zopol</param>
/// <param name="LastCommand">posledny prikaz okrem dotazu (1F, 3F, C0, 00); null = ziadny</param>
public sealed record AmplifierDto(int Band, int State, DateTimeOffset? OnSince, int Switches, int? LastCommand);
