using System.Net;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using TableSimulator.Contracts.Server;
using TableSimulator.Infrastructure.Auth;
using TableSimulator.Infrastructure.Hosting;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Server ako sluzba Windows: parametre instalacie, prikazy sc.exe, kluce ochrany dat citatelne aj uctom sluzby
/// a restart z weboveho rozhrania (len sluzba).
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

        var info = await client.GetFromJsonAsync<ServerInfoDto>("/api/server");
        Assert.IsFalse(info!.Service);
        Assert.AreEqual(server.DataFolder, info.DataFolder);

        var restart = await client.PostAsync("/api/server/restart", null);
        Assert.AreEqual(HttpStatusCode.Conflict, restart.StatusCode);
    }
}
