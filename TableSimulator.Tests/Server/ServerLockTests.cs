using TableSimulator.Infrastructure.Hosting;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Jeden server nad priecinkom nastaveni: druhy zamok nedostane, adresu bieziaceho precita az po jeho starte a po
/// skonceni servera zamok zmizne.
/// </summary>
[TestClass]
public sealed class ServerLockTests
{
    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public async Task Zamok_DruhyServerNedostaneAPrecitaAdresu()
    {
        var folder = Directory.CreateTempSubdirectory("tslock").FullName;
        try
        {
            using (var first = ServerLock.TryAcquire(folder))
            {
                Assert.IsNotNull(first);
                Assert.IsNull(ServerLock.TryAcquire(folder));
                // adresu server zapise az po starte
                Assert.IsNull(await ServerLock.ReadUrlAsync(folder, TimeSpan.Zero));

                first.Publish("http://localhost:5470/");
                Assert.AreEqual("http://localhost:5470/", await ServerLock.ReadUrlAsync(folder, TimeSpan.Zero));
            }

            Assert.IsFalse(File.Exists(Path.Combine(folder, ServerLock.FileName)));
            Assert.IsNull(await ServerLock.ReadUrlAsync(folder, TimeSpan.Zero));
            using var again = ServerLock.TryAcquire(folder);
            Assert.IsNotNull(again);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
