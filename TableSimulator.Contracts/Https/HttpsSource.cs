using System.Text.Json.Serialization;

namespace TableSimulator.Contracts.Https;

/// <summary>Odkial je certifikat HTTPS.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<HttpsSource>))]
public enum HttpsSource
{
    /// <summary>Certifikat vydala certifikacna autorita simulatora - zariadenia jej musia doverovat.</summary>
    [JsonStringEnumMemberName("generated")] Generated,

    /// <summary>Vlastny certifikat zo suboru (napr. od firemnej certifikacnej autority).</summary>
    [JsonStringEnumMemberName("file")] File
}
