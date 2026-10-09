using System.Text;
using System.Text.Json;
using TableSimulator.Application.Alerts;

namespace TableSimulator.Infrastructure.Alerts;

/// <summary>
/// Oznamenie ako POST s JSON: <c>text</c> (predmet a text - citaju ho aj prichodzie webhooky Teams a Slack),
/// <c>subject</c>, <c>event</c> (<c>outage</c>, <c>recovery</c>, <c>test</c>), <c>server</c>, <c>time</c> a <c>lines</c>.
/// </summary>
public sealed class WebhookAlertChannel : IAlertChannel, IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(15) };

    public string Key => "webhook";

    public bool IsConfigured(AlertSettings settings) => settings is { WebhookUrl.Length: > 0 };

    public async Task SendAsync(AlertMessage message, AlertSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(settings);
        var payload = new
        {
            text = message.Subject + "\n" + message.Text,
            subject = message.Subject,
            @event = message.Event,
            server = message.Server,
            time = message.Time,
            lines = message.Lines.Select(l => new { number = l.Number, name = l.Name, status = l.Status, detail = l.Detail, since = l.Since })
        };
        // cele telo s dlzkou - nie vsetky prijimace webhookov zvladnu telo po kuskoch
        using var content = new StringContent(JsonSerializer.Serialize(payload, Json), Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync(new Uri(settings.WebhookUrl), content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _client.Dispose();
}
