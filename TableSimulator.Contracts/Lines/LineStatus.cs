using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Lines;

/// <summary>Stav spojenia linky.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LineStatus>))]
public enum LineStatus
{
    /// <summary>Linka nebezi.</summary>
    [JsonStringEnumMemberName("stopped")] Stopped,

    /// <summary>Linka caka na INISS.</summary>
    [JsonStringEnumMemberName("listening")] Listening,

    /// <summary>INISS je pripojeny.</summary>
    [JsonStringEnumMemberName("connected")] Connected,

    /// <summary>Linku sa nepodarilo spustit (obsadeny port, chybajuci seriovy port).</summary>
    [JsonStringEnumMemberName("error")] Error,

    /// <summary>INISS sa odpojil (TCP, rura) - linka caka, kym sa pripoji znova.</summary>
    [JsonStringEnumMemberName("lost")] Lost,

    /// <summary>Predstierany vypadok linky zo scenara porucha - spojenie je zavrete a nove sa neprijima.</summary>
    [JsonStringEnumMemberName("outage")] Outage
}
