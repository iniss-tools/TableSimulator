using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using TableSimulator.Infrastructure.Auth;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Server simulatora cez TestServer s vlastnym priecinkom nastaveni. Priecinok dostane cez premenne prostredia -
/// triedy, ktore ho pouzivaju, musia mat <see cref="DoNotParallelizeAttribute" />.
/// </summary>
internal sealed class SimulatorServer : WebApplicationFactory<Program>
{
    private SimulatorServer(string dataFolder) => DataFolder = dataFolder;

    /// <summary>Priecinok nastaveni servera (settings.json, config.json, databaza).</summary>
    public string DataFolder { get; }

    /// <summary>Server s nastavenim <paramref name="settingsJson" /> a bez liniek (testy neobsadzuju porty).</summary>
    public static SimulatorServer Start(string settingsJson = "{}")
    {
        var folder = Path.Combine(Path.GetTempPath(), "TableSimulator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "settings.json"), settingsJson);
        File.WriteAllText(Path.Combine(folder, "config.json"), """{"lines":[],"boards":[]}""");
        Environment.SetEnvironmentVariable("TABLESIMULATOR_DATA", folder);
        Environment.SetEnvironmentVariable("TABLESIMULATOR_NO_BROWSER", "1");
        return new SimulatorServer(folder);
    }

    /// <summary>Klient API; bez hlavicky CSRF server zmeny odmietne.</summary>
    public HttpClient Client(bool csrfHeader = true)
    {
        var client = CreateClient();
        if (csrfHeader)
            client.DefaultRequestHeaders.Add(CsrfHeader.HeaderName, "TableSimulator");
        return client;
    }

    // Program si koren obsahu urcuje sam - rovnaky, aby ho TestServer nemenil
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseContentRoot(AppContext.BaseDirectory);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        try
        {
            Directory.Delete(DataFolder, true);
        }
        catch (IOException)
        {
            // subor este drzi iny proces - ostane v docasnom priecinku
        }
    }
}
