using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Signage;

/// <summary>Druh spravy informacneho panelu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SignageMessageKind>))]
public enum SignageMessageKind
{
    /// <summary>Virtualna tabula (prichody/odchody z XML INISSu) - ako sprava P/O v INISSView 2.</summary>
    [JsonStringEnumMemberName("virtual")] Virtual,

    /// <summary>Text v odsekoch.</summary>
    [JsonStringEnumMemberName("text")] Text,

    /// <summary>Obrazok na celu obrazovku.</summary>
    [JsonStringEnumMemberName("image")] Image,

    /// <summary>Simulovana tabula LED.</summary>
    [JsonStringEnumMemberName("board")] Board,

    /// <summary>Webova stranka v ramci.</summary>
    [JsonStringEnumMemberName("web")] Web,

    /// <summary>Video MP4 v slucke.</summary>
    [JsonStringEnumMemberName("video")] Video
}
