using System.Text.Json.Serialization;

namespace TableSimulator.Core.Lines;

/// <summary>
/// Stav spojenia linky.
/// </summary>
public enum LineStatus
{
    [JsonStringEnumMemberName("stopped")] Stopped,
    [JsonStringEnumMemberName("listening")] Listening,
    [JsonStringEnumMemberName("connected")] Connected,
    [JsonStringEnumMemberName("error")] Error,

    /// <summary>INISS sa odpojil (TCP, rura) - linka caka, kym sa pripoji znova.</summary>
    [JsonStringEnumMemberName("lost")] Lost,

    /// <summary>Predstierany vypadok linky (scenar porucha) - spojenie je zavrete a nove sa neprijima.</summary>
    [JsonStringEnumMemberName("outage")] Outage
}
