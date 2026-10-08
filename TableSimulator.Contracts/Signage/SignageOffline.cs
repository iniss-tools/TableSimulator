using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Signage;

/// <summary>Preco panel nic nehra (mimo prevadzky).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SignageOffline>))]
public enum SignageOffline
{
    /// <summary>Panel nema skupinu.</summary>
    [JsonStringEnumMemberName("noGroup")] NoGroup,

    /// <summary>Skupina teraz nema platny playlist ani predvolenu spravu.</summary>
    [JsonStringEnumMemberName("noProgram")] NoProgram,

    /// <summary>Platny playlist nema ziadnu spravu.</summary>
    [JsonStringEnumMemberName("empty")] Empty
}
