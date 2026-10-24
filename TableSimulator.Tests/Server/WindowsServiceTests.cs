using System.Net;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Contracts.Server;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.Hosting;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Server ako sluzba Windows: parametre instalacie, prikazy sc.exe, kluce ochrany dat citatelne aj uctom sluzby,
/// dovod neuspesneho startu, odtlacok servera a restart z weboveho rozhrania (len sluzba).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class WindowsServiceTests
{
    [TestMethod]
    public void PrikazovyRiadok_InstalaciaAOdstranenieSluzby()
    {
        var install = CommandLine.Parse(["--install-service", "--data", @"C:\Data\Sim", "--port", "5480"], AppContext.BaseDirectory);
        Assert.IsTrue(install.InstallService);
        Assert.IsFalse(install.UninstallService);
        Assert.AreEqual(@"C:\Data\Sim", install.Paths.DataFolder);
        Assert.AreEqual(5480, install.Port);

        Assert.IsTrue(CommandLine.Parse(["--uninstall-service"], AppContext.BaseDirectory).UninstallService);
    }

    [TestMethod]
    public void PrikazovyRiadok_SluzbaBezPrehliadacaSPriecinkomPreVsetkych()
    {
        // serverove testy nastavuju priecinok premennou prostredia - tu sa sleduje predvoleny
        var data = Environment.GetEnvironmentVariable("TABLESIMULATOR_DATA");
        Environment.SetEnvironmentVariable("TABLESIMULATOR_DATA", null);
        try
        {
            var line = CommandLine.Parse([], AppContext.BaseDirectory, service: true);

            Assert.IsTrue(line.NoBrowser);
            Assert.AreEqual(WindowsService.DefaultDataFolder, line.Paths.DataFolder);
            // zadany priecinok ma prednost
            Assert.AreEqual(@"D:\Sim", CommandLine.Parse(["--data", @"D:\Sim\"], AppContext.BaseDirectory, service: true).Paths.DataFolder.TrimEnd('\\'));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TABLESIMULATOR_DATA", data);
        }
    }

    [TestMethod]
    public void Instalacia_PrikazovyRiadokSluzbyAPrikazySc()
    {
        var path = WindowsService.BinaryPath(@"C:\Program Files\Sim\TableSimulator.exe", @"C:\ProgramData\TableSimulator\", 5480);
        Assert.AreEqual("\"C:\\Program Files\\Sim\\TableSimulator.exe\" --data \"C:\\ProgramData\\TableSimulator\" --port 5480", path);

        var created = WindowsService.InstallCommands(path, exists: false);
        CollectionAssert.AreEqual(new[] { "create", "TableSimulator", "binPath=", path, "start=", "delayed-auto", "DisplayName=", WindowsService.DisplayName },
            created[0]);
        // restart po zlyhani - aj po ukonceni s kodom roznym od 0 (restart z webu)
        Assert.IsTrue(created.Any(c => c[0] == "failure" && c.Contains("restart/5000/restart/10000/restart/60000")));
        Assert.IsTrue(created.Any(c => c is ["failureflag", "TableSimulator", "1"]));
        Assert.AreEqual("config", WindowsService.InstallCommands(path, exists: true)[0][0]);
        // zastavenu sluzbu spusti odkaz v ponuke Start aj bez prav spravcu - interaktivni pouzivatelia smu len spustit (RP)
        CollectionAssert.AreEqual(new[] { "sdset", "TableSimulator", WindowsService.Security }, created.Single(c => c[0] == "sdset"));
        StringAssert.Contains(WindowsService.Security, "(A;;CCLCSWRPLOCRRC;;;IU)");
        StringAssert.Contains(WindowsService.Security, "(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)");
    }

    [TestMethod]
    public void DovodStartu_ObsadenyPortSRadou()
    {
        var ipv4 = new IOException("Failed to bind to address http://127.0.0.1:5470: address already in use.",
            new AddressInUseException("Only one usage of each socket address is normally permitted."));
        StringAssert.StartsWith(StartupError.Describe(ipv4), "Port 5470 uz pouziva iny program");
        StringAssert.Contains(StartupError.Describe(ipv4), "--port");

        var ipv6 = new IOException("Failed to bind to address https://[::1]:5471: address already in use.", new AddressInUseException("in use"));
        StringAssert.StartsWith(StartupError.Describe(ipv6), "Port 5471 ");
        StringAssert.StartsWith(StartupError.Describe(new AddressInUseException("in use")), "Port webu ");

        // ina chyba - sprava aj s povodnou pricinou
        Assert.AreEqual("Databaza (zamknuta)", StartupError.Describe(new InvalidOperationException("Databaza", new IOException("zamknuta"))));
    }

    [TestMethod]
    public async Task DovodStartu_ZapisCitanieAZmazanie()
    {
        var folder = Directory.CreateTempSubdirectory("tsstart").FullName;
        try
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            Assert.IsNull(StartupError.ReadSince(folder, before));

            StartupError.Write(folder, new InvalidOperationException("Nejde to"));
            Assert.AreEqual("Nejde to", StartupError.ReadSince(folder, before));
            // dovod spred spustenia sluzby neplati
            Assert.IsNull(StartupError.ReadSince(folder, DateTime.UtcNow.AddMinutes(1)));
            // cakanie na server skonci hned s dovodom
            Assert.AreEqual(new ServerStart(null, "Nejde to"), await WindowsService.WaitForServerAsync(folder, before, TimeSpan.FromSeconds(30)));

            StartupError.Clear(folder);
            Assert.IsNull(StartupError.ReadSince(folder, before));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [TestMethod]
    public async Task CakanieNaServer_AdresaZoZamku()
    {
        var folder = Directory.CreateTempSubdirectory("tsstart").FullName;
        try
        {
            using var serverLock = ServerLock.TryAcquire(folder)!;
            serverLock.Publish("http://localhost:5499/");
            Assert.AreEqual(new ServerStart("http://localhost:5499/", null), await WindowsService.WaitForServerAsync(folder, DateTime.UtcNow, TimeSpan.FromSeconds(30)));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [TestMethod]
    public void OdtlacokServera_PocitacAPriecinok()
    {
        var id = ServerIdentity.Of("PC1", @"C:\ProgramData\TableSimulator");
        Assert.AreEqual(16, id.Length);
        // ta ista cesta inak zapisana = ten isty server
        Assert.AreEqual(id, ServerIdentity.Of("pc1", @"c:\programdata\tablesimulator\"));
        Assert.AreNotEqual(id, ServerIdentity.Of("PC1", @"C:\Users\Jano\AppData\Roaming\TableSimulator"));
        Assert.AreNotEqual(id, ServerIdentity.Of("PC2", @"C:\ProgramData\TableSimulator"));
    }

    [TestMethod]
    public void PrikazovyRiadokSluzby_PriecinokNastaveni()
    {
        // podla neho spustenie v okne pozna priecinok sluzby a len otvori bieziaci server
        var path = WindowsService.BinaryPath(@"C:\Program Files\Sim\TableSimulator.exe", @"C:\ProgramData\TableSimulator\", 5480);
        Assert.AreEqual(@"C:\ProgramData\TableSimulator", WindowsService.DataFolderOf(path));
        Assert.IsNull(WindowsService.DataFolderOf("\"C:\\Program Files\\Sim\\TableSimulator.exe\""));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    [SupportedOSPlatform("windows")]
    public void Kluce_PrevodPrePocitacZachovaUdaje()
    {
        var folder = Directory.CreateTempSubdirectory("tskeys");
        try
        {
            // kluc sifrovany pre pouzivatela (ako pred touto verziou)
            var user = Provider(b => b.PersistKeysToFileSystem(folder).ProtectKeysWithDpapi());
            var secret = user.CreateProtector("test").Protect("tajne");

            Assert.AreEqual(1, KeyRingProtection.ToLocalMachine(folder.FullName, out var failed));
            Assert.IsEmpty(failed);
            var text = File.ReadAllText(Directory.GetFiles(folder.FullName, "key-*.xml").Single());
            StringAssert.Contains(text, "DpapiXmlDecryptor");

            var machine = Provider(b => b.PersistKeys(folder.FullName));
            Assert.AreEqual("tajne", machine.CreateProtector("test").Unprotect(secret));
        }
        finally
        {
            folder.Delete(true);
        }
    }

    private static IDataProtectionProvider Provider(Action<IDataProtectionBuilder> configure)
    {
        var services = new ServiceCollection();
        configure(services.AddDataProtection().SetApplicationName("TableSimulator"));
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    [TestMethod]
    public async Task Api_MimoSluzbyBezRestartu()
    {
        await using var server = SimulatorServer.Start();
        var client = server.Client();

        var response = await client.GetAsync("/api/server");
        var info = await response.Content.ReadFromJsonAsync<ServerInfoDto>();
        Assert.IsFalse(info!.Service);
        Assert.AreEqual(server.DataFolder, info.DataFolder);
        // podla odtlacku web pozna, ze na adrese zacal bezat iny server
        Assert.AreEqual(ServerIdentity.Of(Environment.MachineName, server.DataFolder), response.Headers.GetValues(ServerIdentity.Header).Single());

        var restart = await client.PostAsync("/api/server/restart", null);
        Assert.AreEqual(HttpStatusCode.Conflict, restart.StatusCode);
    }
}
