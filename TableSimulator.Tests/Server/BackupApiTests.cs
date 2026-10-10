using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TableSimulator.Contracts.Backup;
using TableSimulator.Contracts.Lines;
using TableSimulator.Contracts.State;

namespace TableSimulator.Tests.Server;

/// <summary>
/// Zaloha a obnova celeho simulatora: ZIP s databazou, vzhladmi, subormi panelov a zaznamami; obnova sa pripravi,
/// pouzije pri dalsom starte servera a povodne udaje odlozi.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BackupApiTests
{
    private static SetLineRequest Pipe(string name) => new() { TableClass = 4, Endpoint = @"\\.\PIPE\TableSimulatorTest-" + Guid.NewGuid().ToString("N"), Name = name };

    private static async Task<string?> CodeOf(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static void Write(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    [TestMethod]
    public async Task ZalohaAObnova_PoRestarteServera()
    {
        string folder;
        byte[] backup;
        await using (var server = SimulatorServer.Start(deleteFolder: false))
        {
            folder = server.DataFolder;
            using var client = server.Client();
            Assert.AreEqual(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/lines/5", Pipe("Zo zalohy"))).StatusCode);
            Write(Path.Combine(folder, "designs", "moj", "design.json"), """{"name":"zaloha"}""");
            Write(Path.Combine(folder, "media", "obrazok.png"), "png");
            Write(Path.Combine(folder, "recordings", "skuska.tsrec"), "zaznam");

            var response = await client.GetAsync("/api/backup/file?recordings=true");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            backup = await response.Content.ReadAsByteArrayAsync();
            using (var zip = new ZipArchive(new MemoryStream(backup)))
            {
                CollectionAssert.IsSubsetOf(new[] { "backup.json", "tablesimulator.db", "designs/moj/design.json", "media/obrazok.png", "recordings/skuska.tsrec" },
                    zip.Entries.Select(e => e.FullName).ToArray());
            }

            // zmeny po zalohe - obnova ich vrati
            Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/lines/5")).StatusCode);
            Write(Path.Combine(folder, "designs", "moj", "design.json"), """{"name":"zmeneny"}""");
            Write(Path.Combine(folder, "designs", "iny", "design.json"), """{"name":"iny"}""");
            File.Delete(Path.Combine(folder, "media", "obrazok.png"));

            // nezmysel a zaloha bez databazy sa odmietnu
            var bad = await client.PostAsync("/api/backup/restore", new ByteArrayContent([1, 2, 3]));
            Assert.AreEqual("backup.format", await CodeOf(bad));
            Assert.IsNull((await client.GetFromJsonAsync<BackupStatusDto>("/api/backup"))!.Pending);

            var restore = await client.PostAsync("/api/backup/restore", new ByteArrayContent(backup));
            Assert.AreEqual(HttpStatusCode.OK, restore.StatusCode, await restore.Content.ReadAsStringAsync());
            var info = (await restore.Content.ReadFromJsonAsync<BackupInfoDto>())!;
            Assert.AreEqual((1, 1, 1), (info.Designs, info.Media, info.Recordings!.Value));
            Assert.IsNotNull((await client.GetFromJsonAsync<BackupStatusDto>("/api/backup"))!.Pending);
        }

        await using (var server = SimulatorServer.Open(folder, true))
        {
            using var client = server.Client();
            var state = (await client.GetFromJsonAsync<StateDto>("/api/state"))!;
            Assert.AreEqual("Zo zalohy", state.Lines.Single().Name);
            StringAssert.Contains(File.ReadAllText(Path.Combine(folder, "designs", "moj", "design.json")), "zaloha");
            // vzhlad, ktory zaloha nema, ostane
            Assert.IsTrue(File.Exists(Path.Combine(folder, "designs", "iny", "design.json")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "media", "obrazok.png")));
            var status = (await client.GetFromJsonAsync<BackupStatusDto>("/api/backup"))!;
            Assert.IsNull(status.Pending);
            Assert.IsNull(status.LastRestore!.Error);
            Assert.AreEqual(1, status.LastRestore.Backup.Designs);

            // povodne udaje su odlozene
            var saved = Directory.GetDirectories(Path.Combine(folder, "before-restore")).Single();
            Assert.AreEqual(saved, status.LastRestore.Folder);
            Assert.IsTrue(File.Exists(Path.Combine(saved, "tablesimulator.db")));
            StringAssert.Contains(File.ReadAllText(Path.Combine(saved, "designs", "moj", "design.json")), "zmeneny");
        }
    }

    [TestMethod]
    public async Task ZrusenieObnovy()
    {
        await using var server = SimulatorServer.Start();
        using var client = server.Client();
        var backup = await (await client.GetAsync("/api/backup/file")).Content.ReadAsByteArrayAsync();
        using (var zip = new ZipArchive(new MemoryStream(backup)))
            Assert.IsFalse(zip.Entries.Any(e => e.FullName.StartsWith("recordings/", StringComparison.Ordinal)));

        Assert.AreEqual(HttpStatusCode.OK, (await client.PostAsync("/api/backup/restore", new ByteArrayContent(backup))).StatusCode);
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/backup/restore")).StatusCode);
        Assert.IsNull((await client.GetFromJsonAsync<BackupStatusDto>("/api/backup"))!.Pending);
        Assert.AreEqual("backup.noPending", await CodeOf(await client.DeleteAsync("/api/backup/restore")));
    }
}
