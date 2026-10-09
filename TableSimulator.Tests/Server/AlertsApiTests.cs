using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.Settings;
using TableSimulator.Contracts.State;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Oznamenia o vypadkoch liniek cez API: ulozenie nastavenia (heslo SMTP len ako priznak), overenie, skusobne
/// oznamenie a vypadok linky dorucene na webhook.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AlertsApiTests
{
    /// <summary>Jednoduchy prijemca webhooku: kazdu poziadavku potvrdi 200 a jej telo da do kanala.</summary>
    private sealed class WebhookReceiver : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        public WebhookReceiver()
        {
            _listener.Start();
            _loop = Task.Run(LoopAsync);
        }

        public System.Threading.Channels.Channel<string> Bodies { get; } = System.Threading.Channels.Channel.CreateUnbounded<string>();

        public string Url => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/hook";

        private async Task LoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    using var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    var stream = client.GetStream();
                    var data = new List<byte>();
                    var buffer = new byte[8192];
                    int length = -1, headerEnd = -1;
                    while (headerEnd < 0 || data.Count < headerEnd + length)
                    {
                        var read = await stream.ReadAsync(buffer, _cts.Token);
                        if (read == 0)
                            break;
                        data.AddRange(buffer.AsSpan(0, read));
                        var text = Encoding.UTF8.GetString(data.ToArray());
                        if (headerEnd < 0 && text.IndexOf("\r\n\r\n", StringComparison.Ordinal) is var end and >= 0)
                        {
                            headerEnd = Encoding.UTF8.GetByteCount(text[..(end + 4)]);
                            var header = text[..end].Split("\r\n").FirstOrDefault(h => h.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                            length = header is null ? 0 : int.Parse(header["Content-Length:".Length..].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }

                    await stream.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(), _cts.Token);
                    await Bodies.Writer.WriteAsync(Encoding.UTF8.GetString(data.ToArray()[headerEnd..]), _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // koniec testu
            }
        }

        public async Task<JsonElement> NextAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            return JsonDocument.Parse(await Bodies.Reader.ReadAsync(timeout.Token)).RootElement.Clone();
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            try
            {
                await _loop;
            }
            catch (Exception e) when (e is SocketException or ObjectDisposedException)
            {
                // zastaveny prijemca
            }

            _cts.Dispose();
        }
    }

    private static AlertSettingsRequest Alerts(string webhook) => new() { Enabled = true, DelaySeconds = 0, WebhookUrl = webhook };

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("errors", out var errors) ? errors[0].GetProperty("code").GetString()
            : json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static async Task SaveAsync(HttpClient client, AlertSettingsRequest alerts)
    {
        var current = (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!;
        var request = new UpdateSettingsRequest
        {
            HttpPort = current.HttpPort, AllowNetwork = current.AllowNetwork, FontsFolder = current.FontsFolder, DesignsFolder = current.DesignsFolder,
            HttpsRedirect = current.HttpsRedirect, Alerts = alerts
        };
        var response = await client.PutAsJsonAsync("/api/settings", request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Nastavenie_HesloLenAkoPriznakAOverenie()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();

        await SaveAsync(client, new AlertSettingsRequest
        {
            Enabled = true, EmailTo = "dispecer@example.com; technik@example.com", EmailFrom = "simulator@example.com", SmtpHost = "smtp.example.com",
            SmtpUser = "simulator", SmtpPassword = "tajne", Language = "cs"
        });

        var settings = (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!.Alerts;
        Assert.IsTrue(settings.SmtpPasswordSet);
        Assert.AreEqual("cs", settings.Language);
        Assert.DoesNotContain("tajne", await client.GetStringAsync("/api/settings"));

        var bad = await client.PostAsJsonAsync("/api/settings/alerts/test", new AlertSettingsRequest { WebhookUrl = "ftp://server" });
        Assert.AreEqual("settings.alertWebhook", await CodeOf(bad));
        var badMail = await client.PostAsJsonAsync("/api/settings/alerts/test", new AlertSettingsRequest { EmailTo = "nie-je-adresa", SmtpHost = "x" });
        Assert.AreEqual("settings.alertEmail", await CodeOf(badMail));
    }

    [TestMethod]
    public async Task Skuska_AVypadokLinkyNaWebhook()
    {
        await using var receiver = new WebhookReceiver();
        await using var server = SimulatorServer.Start();
        using var client = server.Client();

        var test = (await (await client.PostAsJsonAsync("/api/settings/alerts/test", Alerts(receiver.Url))).Content.ReadFromJsonAsync<AlertTestResultDto>())!;
        Assert.IsTrue(test.Webhook.Configured);
        Assert.IsNull(test.Webhook.Error);
        Assert.IsFalse(test.Email.Configured);
        var body = await receiver.NextAsync();
        Assert.AreEqual("test", body.GetProperty("event").GetString());
        StringAssert.Contains(body.GetProperty("text").GetString(), "Skúšobné oznámenie");

        // linka na volnom porte: INISS sa pripoji a odpoji
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        await client.PutAsJsonAsync("/api/lines/9", new SetLineRequest { TableClass = 4, Endpoint = $"TCP://127.0.0.1:{port}", Name = "Hala" });
        await SaveAsync(client, Alerts(receiver.Url));
        for (var i = 0; i < 50 && (await client.GetFromJsonAsync<StateDto>("/api/state"))!.Lines.SingleOrDefault()?.Status != LineStatus.Listening; i++)
            await Task.Delay(100);
        using (var iniss = new TcpClient())
        {
            await iniss.ConnectAsync(IPAddress.Loopback, port);
            await Task.Delay(500);
        }

        var outage = await receiver.NextAsync();
        Assert.AreEqual("outage", outage.GetProperty("event").GetString());
        var line = outage.GetProperty("lines")[0];
        Assert.AreEqual(9, line.GetProperty("number").GetInt32());
        Assert.AreEqual("lost", line.GetProperty("status").GetString());
        StringAssert.Contains(outage.GetProperty("text").GetString(), "Linka 9 – Hala: INISS sa odpojil");

        using (var again = new TcpClient())
        {
            await again.ConnectAsync(IPAddress.Loopback, port);
            var recovery = await receiver.NextAsync();
            Assert.AreEqual("recovery", recovery.GetProperty("event").GetString());
        }
    }
}
